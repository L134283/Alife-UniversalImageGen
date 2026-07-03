using System;
using System.IO;
using System.Linq;
using Alife.Framework;
using Alife.Platform;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AntDesign;

namespace Alife.Plugin.ImageGen.Universal;

public partial class UniversalImageGenUI : ModuleUIBase<UniversalImageGen, UniversalImageGenConfig>
{
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        if (Configuration == null)
        {
            b.AddContent(0, "Configuration NULL");
            return;
        }

        int i = 0;

        // ===== 容器 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style",
            "background:#fafafa;padding:24px;border-radius:12px;border:1px solid #f0f0f0;max-width:680px;");

        // ===== 标题 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "font-size:18px;font-weight:bold;margin-bottom:4px;");
        b.AddContent(i++, "🖼️ 通用生图");
        b.CloseElement();

        // ===== 使用说明 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style",
            "font-size:12px;color:#555;background:#e6f7ff;padding:10px 12px;border-radius:8px;margin-bottom:16px;line-height:1.7;");
        b.AddContent(i++, "📌 让AI根据描述生成或修改图片，AI会通过函数序号选择使用哪组 API\n");
        b.AddContent(i++, "📌 修改配置后需重新加载模块（设置→插件→刷新）\n");
        b.AddContent(i++, "📌 图生图需要 API 服务商额外支持，openai 格式仅部分第三方兼容");
        b.CloseElement();

        // ===== 主 API 配置 =====
        SectionTitle(b, ref i, "📋 主 API 配置（第 1 组）");

        AddPresetButtons(b, ref i);

        BuildGroupFields(b, ref i,
            "Api1_",
            () => Configuration.Api1_Name, v => Configuration.Api1_Name = v,
            () => Configuration.Api1_Type, v => Configuration.Api1_Type = v,
            () => Configuration.Api1_Endpoint, v => Configuration.Api1_Endpoint = v,
            () => Configuration.Api1_ApiKey, v => Configuration.Api1_ApiKey = v,
            () => Configuration.Api1_Model, v => Configuration.Api1_Model = v,
            () => Configuration.Api1_DefaultSize, v => Configuration.Api1_DefaultSize = v);

        // ===== 备用 API（折叠） =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");

        SectionTitle(b, ref i, "📋 备用 API（可选，点击展开）");

        AddCollapsibleGroup(b, ref i, "API 组 2", () =>
            BuildGroupFields(b, ref i,
                "Api2_",
                () => Configuration.Api2_Name ?? "", v => Configuration.Api2_Name = v,
                () => Configuration.Api2_Type ?? "openai", v => Configuration.Api2_Type = v,
                () => Configuration.Api2_Endpoint ?? "", v => Configuration.Api2_Endpoint = v,
                () => Configuration.Api2_ApiKey ?? "", v => Configuration.Api2_ApiKey = v,
                () => Configuration.Api2_Model ?? "", v => Configuration.Api2_Model = v,
                () => Configuration.Api2_DefaultSize ?? "1024x1024", v => Configuration.Api2_DefaultSize = v));

        AddCollapsibleGroup(b, ref i, "API 组 3", () =>
            BuildGroupFields(b, ref i,
                "Api3_",
                () => Configuration.Api3_Name ?? "", v => Configuration.Api3_Name = v,
                () => Configuration.Api3_Type ?? "openai", v => Configuration.Api3_Type = v,
                () => Configuration.Api3_Endpoint ?? "", v => Configuration.Api3_Endpoint = v,
                () => Configuration.Api3_ApiKey ?? "", v => Configuration.Api3_ApiKey = v,
                () => Configuration.Api3_Model ?? "", v => Configuration.Api3_Model = v,
                () => Configuration.Api3_DefaultSize ?? "1024x1024", v => Configuration.Api3_DefaultSize = v));

        AddCollapsibleGroup(b, ref i, "API 组 4", () =>
            BuildGroupFields(b, ref i,
                "Api4_",
                () => Configuration.Api4_Name ?? "", v => Configuration.Api4_Name = v,
                () => Configuration.Api4_Type ?? "openai", v => Configuration.Api4_Type = v,
                () => Configuration.Api4_Endpoint ?? "", v => Configuration.Api4_Endpoint = v,
                () => Configuration.Api4_ApiKey ?? "", v => Configuration.Api4_ApiKey = v,
                () => Configuration.Api4_Model ?? "", v => Configuration.Api4_Model = v,
                () => Configuration.Api4_DefaultSize ?? "1024x1024", v => Configuration.Api4_DefaultSize = v));

        b.CloseElement();

        // ===== 保存设置 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");

        SectionTitle(b, ref i, "📂 保存设置");

        var defaultSaveDir = Path.Combine(AlifePath.StorageFolderPath, "Images", "Generated");
        var currentSaveDir = string.IsNullOrWhiteSpace(Configuration.SaveDirectory)
            ? defaultSaveDir
            : Configuration.SaveDirectory;

        AddInput(b, ref i, "图片保存目录", Configuration.SaveDirectory, v => Configuration.SaveDirectory = v);
        AddHint(b, ref i, $"当前保存位置：{currentSaveDir}　留空则使用默认目录");
        b.CloseElement();

        // ===== 关闭容器 =====
        b.CloseElement();
    }

    // ======================================================================
    // 预设按钮
    // ======================================================================

    void AddPresetButtons(RenderTreeBuilder b, ref int seq)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "display:flex;flex-wrap:wrap;gap:6px;margin:6px 0 10px;");

        PresetBtn(b, ref seq, "SiliconFlow", "openai",
            "https://api.siliconflow.cn/v1/images/generations", "black-forest-flux/1");
        PresetBtn(b, ref seq, "OpenAI", "openai",
            "https://api.openai.com/v1/images/generations", "dall-e-3");
        PresetBtn(b, ref seq, "通义万相", "wan",
            "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation", "wan2.7-image");
        PresetBtn(b, ref seq, "Agnes", "agnes",
            "https://apihub.agnes-ai.com/v1/images/generations", "");
        PresetBtn(b, ref seq, "清空", "", "", "");

        b.CloseElement();
    }

    void PresetBtn(RenderTreeBuilder b, ref int seq, string label, string type, string endpoint, string model)
    {
        var isClear = label == "清空";
        b.OpenElement(seq++, "button");
        b.AddAttribute(seq++, "type", "button");
        b.AddAttribute(seq++, "style",
            "padding:3px 10px;border:1px solid #d9d9d9;border-radius:4px;" +
            "cursor:pointer;font-size:12px;background:#fafafa;transition:all 0.15s;" +
            "font-family:inherit;" +
            (isClear ? "color:#999;" : "color:#333;"));
        b.AddAttribute(seq++, "onmouseover",
            "this.style.background='#e6f7ff';this.style.borderColor='#91d5ff'");
        b.AddAttribute(seq++, "onmouseout",
            "this.style.background='#fafafa';this.style.borderColor='#d9d9d9'");
        b.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () =>
        {
            Configuration.Api1_Type = type;
            Configuration.Api1_Endpoint = endpoint;
            Configuration.Api1_Model = model;
            if (isClear)
            {
                Configuration.Api1_Name = "默认";
                Configuration.Api1_Type = "agnes";
                Configuration.Api1_ApiKey = "";
                Configuration.Api1_DefaultSize = "1024x1024";
            }
        }));
        b.AddContent(seq++, isClear ? "↺ 清空" : label);
        b.CloseElement();
    }

    // ======================================================================
    // 折叠组
    // ======================================================================

    void AddCollapsibleGroup(RenderTreeBuilder b, ref int seq, string title, Action renderContent)
    {
        b.OpenElement(seq++, "details");
        b.AddAttribute(seq++, "style",
            "margin:4px 0;border:1px solid #e8e8e8;border-radius:6px;padding:6px 10px;background:#fff;");

        b.OpenElement(seq++, "summary");
        b.AddAttribute(seq++, "style",
            "cursor:pointer;font-weight:bold;font-size:13px;color:#666;padding:2px 0;user-select:none;");
        b.AddContent(seq++, $"⬇️ {title}");
        b.CloseElement();

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style", "padding:8px 0 4px;");

        renderContent();

        b.CloseElement();
        b.CloseElement();
    }

    // ======================================================================
    // 分组字段
    // ======================================================================

    void BuildGroupFields(RenderTreeBuilder b, ref int seq,
        string prefix,
        Func<string> getName, Action<string> setName,
        Func<string> getType, Action<string> setType,
        Func<string> getEndpoint, Action<string> setEndpoint,
        Func<string> getKey, Action<string> setKey,
        Func<string> getModel, Action<string> setModel,
        Func<string> getSize, Action<string> setSize)
    {
        bool isGroup1 = prefix == "Api1_";

        // 名称
        AddInput(b, ref seq, "名称", getName(), setName);
        AddHint(b, ref seq, "仅用于AI识别是哪组 API，可随便填");

        // 接口类型（下拉）
        if (isGroup1)
        {
            AddTypeSelect(b, ref seq, "接口类型", getType(), setType);
            AddHint(b, ref seq, "根据 API 服务商选择：openai（通用格式）/ agnes / wan（通义万相）\n" +
                "图生图推荐 agnes 或 wan 格式，openai 格式仅部分第三方兼容");
        }
        else
        {
            AddTypeSelect(b, ref seq, "接口类型", getType(), setType);
            AddHint(b, ref seq, "openai / agnes / wan 三种格式");
        }

        // Endpoint
        AddInput(b, ref seq, "API Endpoint", getEndpoint(), setEndpoint);
        AddHint(b, ref seq, "完整的 API 接口地址，注意区分文生图和图生图的接口");

        // API Key（密码框）
        AddPassword(b, ref seq, "API Key", getKey(), setKey);
        AddHint(b, ref seq, "API 密钥，不会明文显示");

        // 模型
        AddInput(b, ref seq, "模型名称", getModel(), setModel);
        AddHint(b, ref seq, "例如：dall-e-3 / black-forest-flux/1 / wanx2.1-t2i-turbo");

        // 尺寸
        AddInput(b, ref seq, "默认尺寸", getSize(), setSize);
        AddHint(b, ref seq, "格式：宽x高，如 1024x1024。调用函数时可单独指定宽高覆盖此值");
    }

    // ======================================================================
    // 工具函数
    // ======================================================================

    void SectionTitle(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "font-size:14px;font-weight:bold;color:#555;margin:0 0 8px;border-bottom:1px solid #e0e0e0;padding-bottom:4px;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddHint(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "font-size:11px;color:#999;margin:0 0 10px 2px;line-height:1.5;white-space:pre-line;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddLabel(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "font-weight:bold;margin-bottom:3px;font-size:13px;color:#444;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddInput(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenComponent<Input<string>>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();
    }

    void AddPassword(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenComponent<InputPassword>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();
    }

    void AddTypeSelect(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);

        b.OpenElement(seq++, "select");
        b.AddAttribute(seq++, "style",
            "width:100%;padding:6px 10px;border:1px solid #d9d9d9;border-radius:6px;" +
            "font-size:13px;background:#fff;font-family:inherit;color:#333;");
        b.AddAttribute(seq++, "value", value);
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value?.ToString() ?? "openai")));

        var options = new[] {
            ("openai", "OpenAI 兼容格式"),
            ("agnes", "Agnes 格式"),
            ("wan", "通义万相格式"),
        };
        foreach (var (val, text) in options)
        {
            b.OpenElement(seq++, "option");
            b.AddAttribute(seq++, "value", val);
            if (val == value)
                b.AddAttribute(seq++, "selected", true);
            b.AddContent(seq++, text);
            b.CloseElement();
        }
        b.CloseElement();
    }
}
