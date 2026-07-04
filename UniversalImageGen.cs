using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Alife.Framework;
using Alife.Function.FunctionCaller;
using Alife.Function.Interpreter;
using Alife.Platform;

namespace Alife.Plugin.ImageGen.Universal;

[Module(
    "通用生图",
    "通用生图插件，支持最多 4 组 API 配置，兼容 OpenAI / Agnes / Wan 三种格式，支持文生图和图生图。\n本插件功能由 初心、爱奈丽实现，本人仅略加优化",
    defaultCategory: "Doro的妙妙工具",
    EditorUI = typeof(UniversalImageGenUI))]
public class UniversalImageGen(
    XmlFunctionCaller functionService
) : InteractiveModule<UniversalImageGen>, IConfigurable<UniversalImageGenConfig>
{
    private static readonly HttpClient _http = new(new HttpClientHandler { UseProxy = false })
        { Timeout = TimeSpan.FromSeconds(180) };

    private static readonly HttpClient _dlHttp = new()
        { Timeout = TimeSpan.FromSeconds(60) };

    private const int MaxDownloadBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/gif", "image/bmp"
    };

    public UniversalImageGenConfig? Configuration { get; set; } = new();

    static void Log(string msg) => Console.WriteLine($"[通用生图] {msg}");

    public override async Task AwakeAsync(AwakeContext context)
    {
        await base.AwakeAsync(context);
        var handler = new XmlHandler(this);
        functionService.RegisterHandler(handler);

        var cfg = Configuration ?? new UniversalImageGenConfig();
        var saveDir = string.IsNullOrWhiteSpace(cfg.SaveDirectory)
            ? Path.Combine(AlifePath.StorageFolderPath, "Images", "Generated")
            : cfg.SaveDirectory;

        var activeGroups = new[] {
            (1, cfg.Api1_ApiKey), (2, cfg.Api2_ApiKey),
            (3, cfg.Api3_ApiKey), (4, cfg.Api4_ApiKey)
        }.Where(g => !string.IsNullOrWhiteSpace(g.Item2))
         .Select(g => g.Item1)
         .ToList();

        var groupHint = activeGroups.Count > 0
            ? $"当前已配置的 API 组：第 {string.Join(" 组、第 ", activeGroups)} 组"
            : "当前未配置任何 API，请先在插件设置中填写";

        Prompt($$"""
            此服务支持 AI 图片生成功能。你可以让我根据描述生成图片。
            {{groupHint}}，通过函数序号选择使用对应的 API 组。
            生成的图片会保存到：{{saveDir}}，你可以读取该目录查看已生成的图片。
            传图片链接也可作为参考图进行图生图。

            若当前在 QQ 聊天环境中，生图完成后直接用以下标签将图片发送到 QQ：
            <qimage image="{{saveDir}}/文件名.后缀" />
            """);
    }

    // ==============================
    // GenerateImage_1 ~ _4
    // ==============================

    [XmlFunction(FunctionMode.OneShot)]
    [Description("使用第 1 组 API 生成或编辑图片")]
    public void GenerateImage_1(
        [Description("图片描述提示词，英文更佳")] string prompt,
        [Description("图片路径或链接，传入则进行图生图")] string? img = null,
        [Description("图片宽度，默认 1024")] int? width = null,
        [Description("图片高度，默认 1024")] int? height = null)
    {
        Poke("本轮请求已发出成功，可以继续聊天");
        _ = GenerateImageAsync(1, prompt, img, width, height);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("使用第 2 组 API 生成或编辑图片")]
    public void GenerateImage_2(
        [Description("图片描述提示词，英文更佳")] string prompt,
        [Description("图片路径或链接，传入则进行图生图")] string? img = null,
        [Description("图片宽度，默认 1024")] int? width = null,
        [Description("图片高度，默认 1024")] int? height = null)
    {
        Poke("本轮请求已发出成功，可以继续聊天");
        _ = GenerateImageAsync(2, prompt, img, width, height);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("使用第 3 组 API 生成或编辑图片")]
    public void GenerateImage_3(
        [Description("图片描述提示词，英文更佳")] string prompt,
        [Description("图片路径或链接，传入则进行图生图")] string? img = null,
        [Description("图片宽度，默认 1024")] int? width = null,
        [Description("图片高度，默认 1024")] int? height = null)
    {
        Poke("本轮请求已发出成功，可以继续聊天");
        _ = GenerateImageAsync(3, prompt, img, width, height);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("使用第 4 组 API 生成或编辑图片")]
    public void GenerateImage_4(
        [Description("图片描述提示词，英文更佳")] string prompt,
        [Description("图片路径或链接，传入则进行图生图")] string? img = null,
        [Description("图片宽度，默认 1024")] int? width = null,
        [Description("图片高度，默认 1024")] int? height = null)
    {
        Poke("本轮请求已发出成功，可以继续聊天");
        _ = GenerateImageAsync(4, prompt, img, width, height);
    }

    // ==============================
    // 核心逻辑
    // ==============================

    async Task GenerateImageAsync(int apiIndex, string prompt, string? imgPath, int? width, int? height)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Poke("提示词不能为空");
            return;
        }

        var (endpoint, key, model, apiType, defaultSize) = GetApiConfig(apiIndex);
        if (string.IsNullOrWhiteSpace(key))
        {
            Poke($"第 {apiIndex} 组 API 未配置，请先在插件设置中填写");
            return;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Poke($"第 {apiIndex} 组 API 未配置 Endpoint");
            return;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            Poke($"第 {apiIndex} 组 API 未配置模型名称");
            return;
        }

        var (dw, dh) = ParseSize(defaultSize ?? "1024x1024");
        var w = Math.Clamp(width ?? dw, 256, 4096);
        var h = Math.Clamp(height ?? dh, 256, 4096);
        var resolvedSize = $"{w}x{h}";

        var saveDir = string.IsNullOrWhiteSpace(Configuration?.SaveDirectory)
            ? Path.Combine(AlifePath.StorageFolderPath, "Images", "Generated")
            : Configuration.SaveDirectory;

        Directory.CreateDirectory(saveDir);

        // 处理参考图（支持 URL 和本地路径）
        var effectiveImgPath = await PrepareReferenceImageAsync(imgPath, saveDir);

        try
        {
            Log($"生图请求 [{apiIndex}] {apiType} {model}");
            if (effectiveImgPath != null)
                Log($"参考图: {Path.GetFileName(effectiveImgPath)}");

            string? imageUrl = null;
            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    imageUrl = (apiType?.ToLower()) switch
                    {
                        "agnes" => await CallAgnesAsync(endpoint, key, model, prompt, effectiveImgPath, resolvedSize),
                        "wan" => await CallWanAsync(endpoint, key, model, prompt, effectiveImgPath, resolvedSize),
                        _ => await CallOpenAIAsync(endpoint, key, model, prompt, effectiveImgPath, resolvedSize),
                    };

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                        break;

                    Log($"请求返回空（请检查 API 配置）");
                    Poke($"第 {apiIndex} 组 API 请求失败");
                    return;
                }
                catch (HttpRequestException ex) when (ex.StatusCode.HasValue && (int)ex.StatusCode.Value < 500)
                {
                    Log($"请求错误 ({(int)ex.StatusCode.Value})");
                    if (attempt >= maxRetries)
                        throw;
                    if (attempt < maxRetries)
                        Log($"重试 ({attempt + 1}/{maxRetries})...");
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Log($"连接失败: {ex.Message}");
                    if (attempt >= maxRetries)
                        throw;
                    if (attempt < maxRetries)
                        Log($"重试 ({attempt + 1}/{maxRetries})...");
                    await Task.Delay(2000);
                }
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                Log($"{maxRetries} 次重试均失败");
                Poke("API 请求失败，请查看日志");
                return;
            }

            // 下载生成的图片（大小 + 类型校验）
            var imgData = await DownloadImageAsync(imageUrl);
            if (imgData == null)
                return;

            var ext = await DetectExtensionAsync(imageUrl, imgData);
            var fileName = $"universal_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(saveDir, fileName);
            await File.WriteAllBytesAsync(savePath, imgData);

            Log($"保存完成 ({imgData.Length / 1024.0:F0}KB) -> {fileName}");
            Poke($"图片已生成\n{savePath}");
        }
        catch (TaskCanceledException)
        {
            Poke("请求超时，请检查接口地址或稍后重试");
        }
        catch (HttpRequestException ex)
        {
            Poke($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log($"生成异常: {ex.Message}");
            Poke($"生成失败: {ex.Message}");
        }
    }

    async Task<string?> PrepareReferenceImageAsync(string? imgPath, string saveDir)
    {
        if (string.IsNullOrWhiteSpace(imgPath))
            return null;

        // URL 参考图：下载到保存目录
        if (imgPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imgPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Log($"下载参考图: {imgPath}");
            try
            {
                var imgData = await DownloadImageAsync(imgPath);
                if (imgData == null)
                {
                    Log($"参考图下载失败");
                    return null;
                }

                var ext = await DetectExtensionAsync(imgPath, imgData);
                var refName = $"ref_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var refPath = Path.Combine(saveDir, refName);
                await File.WriteAllBytesAsync(refPath, imgData);
                Log($"参考图已缓存 ({imgData.Length / 1024.0:F0}KB) -> {refName}");
                return refPath;
            }
            catch (Exception ex)
            {
                Log($"参考图下载失败: {ex.Message}");
                return null;
            }
        }

        // 本地路径
        if (File.Exists(imgPath))
            return imgPath;

        Log($"参考图不存在: {imgPath}");
        Poke($"图片文件不存在 ({imgPath})，将以文生图模式生成");
        return null;
    }

    async Task<byte[]?> DownloadImageAsync(string url)
    {
        // data: URI 直接解析 base64
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var comma = url.IndexOf(',');
                if (comma < 0) return null;
                var b64 = url.Substring(comma + 1);
                var data = Convert.FromBase64String(b64);
                Log($"从 data: URI 解析 ({data.Length / 1024.0:F0}KB)");
                return data;
            }
            catch (Exception ex)
            {
                Log($"data: URI 解析失败: {ex.Message}");
                return null;
            }
        }

        try
        {
            using var resp = await _dlHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!resp.IsSuccessStatusCode)
            {
                Log($"下载失败 (HTTP {(int)resp.StatusCode})");
                Poke($"下载图片失败 (HTTP {(int)resp.StatusCode})");
                return null;
            }

            var contentType = resp.Content.Headers.ContentType?.MediaType;
            if (contentType != null && !AllowedContentTypes.Contains(contentType))
            {
                Log($"类型不符: {contentType}");
                Poke($"API 返回的不是图片 (Content-Type: {contentType})");
                return null;
            }

            var contentLength = resp.Content.Headers.ContentLength;
            if (contentLength > MaxDownloadBytes)
            {
                Poke($"图片过大 ({contentLength / 1024.0 / 1024:F1}MB)，限制为 {MaxDownloadBytes / 1024 / 1024}MB");
                return null;
            }

            var data = await resp.Content.ReadAsByteArrayAsync();
            if (data.Length > MaxDownloadBytes)
            {
                Poke($"图片过大 ({data.Length / 1024.0 / 1024:F1}MB)，限制为 {MaxDownloadBytes / 1024 / 1024}MB");
                return null;
            }

            return data;
        }
        catch (Exception ex)
        {
            Log($"下载异常: {ex.Message}");
            Poke($"下载图片失败: {ex.Message}");
            return null;
        }
    }

    static async Task<string> DetectExtensionAsync(string url, byte[] data)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp")
                return ext == ".jpeg" ? ".jpg" : ext;
        }
        catch { }

        if (data.Length >= 8)
        {
            if (data[0] == 0x89 && data[1] == 0x50) return ".png";
            if (data[0] == 0xFF && data[1] == 0xD8) return ".jpg";
            if (data[0] == 0x47 && data[1] == 0x49) return ".gif";
            if (data[0] == 0x52 && data[1] == 0x49) return ".webp";
            if (data[0] == 0x42 && data[1] == 0x4D) return ".bmp";
        }
        if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x00 && data[3] == 0x1C)
            return ".ico";

        return ".png";
    }

    static (int w, int h) ParseSize(string size)
    {
        var parts = size.Split('x', 'X');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var w) &&
            int.TryParse(parts[1], out var h))
            return (w, h);
        return (1024, 1024);
    }

    // ==============================
    // API 调用
    // ==============================

    async Task<string?> CallOpenAIAsync(string endpoint, string key, string model, string prompt, string? imgPath, string size)
    {
        var isChat = endpoint.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        if (isChat)
        {
            var msg = new JsonObject { ["role"] = "user" };
            if (imgPath != null)
            {
                var b64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imgPath));
                var ext = Path.GetExtension(imgPath)?.TrimStart('.').ToLowerInvariant() ?? "png";
                msg["content"] = new JsonArray
                {
                    new JsonObject { ["type"] = "text", ["text"] = prompt },
                    new JsonObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JsonObject { ["url"] = $"data:image/{ext};base64,{b64}" }
                    }
                };
            }
            else
            {
                msg["content"] = prompt;
            }

            req.Content = new StringContent(new JsonObject
            {
                ["model"] = model,
                ["messages"] = new JsonArray { msg },
            }.ToJsonString(), Encoding.UTF8, "application/json");
        }
        else
        {
            var body = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["prompt"] = prompt,
                ["n"] = 1,
                ["size"] = size,
            };

            if (imgPath != null)
            {
                var b64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imgPath));
                var ext = Path.GetExtension(imgPath)?.TrimStart('.').ToLowerInvariant() ?? "png";
                body["image"] = $"data:image/{ext};base64,{b64}";
            }

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var resp = await _http.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var code = (int)resp.StatusCode;
            Log($"请求失败 (HTTP {code}): {(raw.Length > 200 ? raw[..200] + "..." : raw)}");
            if (code >= 500)
                throw new HttpRequestException($"服务端错误 ({code})", null, resp.StatusCode);
            return null;
        }

        var node = JsonNode.Parse(raw);
        if (node == null)
        {
            Log("响应解析失败: 返回空节点");
            return null;
        }

        if (isChat)
        {
            var content = node["choices"]?[0]?["message"]?["content"];
            if (content is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    var url = item?["image_url"]?["url"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }
            }
            var text = content?.GetValue<string>();
            if (text != null)
            {
                var m = Regex.Match(text, @"!\[.*?\]\(([^\s\)]+)\)");
                if (m.Success) return m.Groups[1].Value;
                m = Regex.Match(text, @"(data:image/[^\s;]+;base64[^\s\)]+)");
                if (m.Success) return m.Groups[1].Value;
                m = Regex.Match(text, @"(https?://[^\s\)]+\.(?:png|jpg|jpeg|webp|gif))");
                if (m.Success) return m.Groups[1].Value;
            }
            // 尝试 images/generations 格式的响应
            var altUrl = node["data"]?[0]?["url"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(altUrl))
                return altUrl;
            // 显示前 300 字符用于诊断
            var preview = raw.Length > 300 ? raw[..300] + "..." : raw;
            Log($"chat 响应未找到图片链接: {preview}");
            return null;
        }

        return node["data"]?[0]?["url"]?.GetValue<string>();
    }

    async Task<string?> CallAgnesAsync(string endpoint, string key, string model, string prompt, string? imgPath, string size)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        var body = new JsonObject
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["n"] = 1,
            ["size"] = size,
            ["extra_body"] = new JsonObject
            {
                ["response_format"] = "url"
            }
        };

        if (imgPath != null)
        {
            var b64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imgPath));
            var ext = Path.GetExtension(imgPath)?.TrimStart('.').ToLowerInvariant() ?? "png";
            body["extra_body"]!["image"] = new JsonArray { $"data:image/{ext};base64,{b64}" };
        }

        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var code = (int)resp.StatusCode;
            Log($"请求失败 (HTTP {code}): {(raw.Length > 200 ? raw[..200] + "..." : raw)}");
            if (code >= 500)
                throw new HttpRequestException($"服务端错误 ({code})", null, resp.StatusCode);
            return null;
        }

        var node = JsonNode.Parse(raw);
        return node?["data"]?[0]?["url"]?.GetValue<string>();
    }

    async Task<string?> CallWanAsync(string endpoint, string key, string model, string prompt, string? imgPath, string size)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        if (!endpoint.Contains("/services/aigc/multimodal-generation/generation", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(endpoint.TrimEnd('/'));
            if (uri.AbsolutePath.Length <= 1 || uri.AbsolutePath == "/v1" || uri.AbsolutePath == "/api")
                endpoint = endpoint.TrimEnd('/') + "/services/aigc/multimodal-generation/generation";
        }

        var contentList = new JsonArray();
        if (imgPath != null)
        {
            var b64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imgPath));
            var ext = Path.GetExtension(imgPath)?.TrimStart('.').ToLowerInvariant() ?? "png";
            contentList.Add(new JsonObject { ["image"] = $"data:image/{ext};base64,{b64}" });
        }
        contentList.Add(new JsonObject { ["text"] = prompt });

        var body = new JsonObject
        {
            ["model"] = model,
            ["input"] = new JsonObject
            {
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = contentList
                    }
                }
            },
            ["parameters"] = new JsonObject
            {
                ["n"] = 1,
                ["size"] = size.Replace('x', '*'),
                ["watermark"] = false
            }
        };

        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var code = (int)resp.StatusCode;
            Log($"Wan 请求失败 (HTTP {code}): {(raw.Length > 200 ? raw[..200] + "..." : raw)}");
            if (code >= 500)
                throw new HttpRequestException($"服务端错误 ({code})", null, resp.StatusCode);
            return null;
        }

        var node = JsonNode.Parse(raw);
        if (node == null)
        {
            Log("Wan 响应解析失败: 返回空节点");
            return null;
        }
        var imageUrl = node["output"]?["choices"]?[0]?["message"]?["content"]?[0]?["image"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(imageUrl))
            return imageUrl;
        var taskStatus = node["output"]?["task_status"]?.GetValue<string>();
        if (taskStatus != null)
            Log($"Wan 任务状态: {taskStatus}");
        var preview = raw.Length > 300 ? raw[..300] + "..." : raw;
        Log($"Wan 响应未找到图片: {preview}");
        return null;
    }

    // ==============================
    // 配置读取
    // ==============================

    (string? endpoint, string? key, string? model, string? type, string? size) GetApiConfig(int index)
    {
        var c = Configuration;
        if (c == null) return (null, null, null, null, null);

        return index switch
        {
            1 => (c.Api1_Endpoint, c.Api1_ApiKey, c.Api1_Model, c.Api1_Type, c.Api1_DefaultSize),
            2 => (c.Api2_Endpoint, c.Api2_ApiKey, c.Api2_Model, c.Api2_Type, c.Api2_DefaultSize),
            3 => (c.Api3_Endpoint, c.Api3_ApiKey, c.Api3_Model, c.Api3_Type, c.Api3_DefaultSize),
            4 => (c.Api4_Endpoint, c.Api4_ApiKey, c.Api4_Model, c.Api4_Type, c.Api4_DefaultSize),
            _ => (null, null, null, null, null),
        };
    }
}
