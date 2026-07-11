using System;
using System.IO;
using System.Linq;
using Alife.Framework;
using Alife.Platform;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;

namespace Alife.Plugin.ImageGen.Universal;

public partial class UniversalImageGenUI : ModuleUIBase<UniversalImageGen, UniversalImageGenConfig>
{
    const string Css = @"
/* ===== 容器 ===== */
.uig-container {
    background: linear-gradient(135deg, #fff0f5 0%, #fce4ec 50%, #fff0f5 100%);
    padding: 28px;
    border-radius: 16px;
    border: 1px solid #ffc1d6;
    max-width: 680px;
    box-shadow: 0 8px 32px rgba(255,107,157,0.12), 0 2px 8px rgba(255,107,157,0.08);
    animation: uig-fade-in 0.4s ease-out;
}
@keyframes uig-fade-in {
    from { opacity: 0; transform: translateY(12px); }
    to { opacity: 1; transform: translateY(0); }
}

/* ===== 标题 ===== */
.uig-title {
    font-size: 22px;
    font-weight: 800;
    margin-bottom: 16px;
    background: linear-gradient(90deg, #ff6b9d, #ff1493, #ff85b3, #ff6b9d);
    background-size: 200% auto;
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
    animation: uig-shimmer 3s linear infinite;
}
@keyframes uig-shimmer {
    0% { background-position: -200% center; }
    100% { background-position: 200% center; }
}

/* ===== 信息框 ===== */
.uig-alert {
    background: linear-gradient(135deg, #fff0f5, #fce4ec);
    border-left: 4px solid #ff6b9d;
    border-radius: 10px;
    padding: 12px 16px;
    margin-bottom: 16px;
    display: flex;
    gap: 10px;
    align-items: flex-start;
}
.uig-alert-icon { font-size: 18px; line-height: 1.6; flex-shrink: 0; }
.uig-alert-title { font-weight: 700; color: #d63384; margin-bottom: 4px; font-size: 13px; }
.uig-alert-desc { font-size: 12px; color: #ad5377; line-height: 1.7; white-space: pre-line; }

/* ===== 分区标题 ===== */
.uig-section {
    font-size: 15px;
    font-weight: 700;
    color: #d63384;
    margin: 22px 0 10px;
    padding-bottom: 6px;
    border-bottom: 2px solid;
    border-image: linear-gradient(90deg, #ff6b9d, rgba(255,107,157,0.1)) 1;
    display: flex;
    align-items: center;
    gap: 6px;
}

/* ===== 状态徽标 ===== */
.uig-badge-on {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 3px 12px;
    border-radius: 12px;
    font-size: 11px;
    font-weight: 600;
    background: linear-gradient(135deg, #ff6b9d, #ff1493);
    color: #fff;
    box-shadow: 0 2px 8px rgba(255,20,147,0.3);
}
.uig-badge-on::before { content: '\25CF'; animation: uig-pulse 2s ease-in-out infinite; }
.uig-badge-off {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 3px 12px;
    border-radius: 12px;
    font-size: 11px;
    font-weight: 600;
    background: #f0f0f0;
    color: #aaa;
}
.uig-badge-off::before { content: '\25CB'; }
@keyframes uig-pulse {
    0%,100% { opacity: 1; transform: scale(1); }
    50% { opacity: 0.5; transform: scale(0.8); }
}

/* ===== 预设按钮 ===== */
.uig-preset-row {
    display: flex; flex-wrap: wrap; gap: 8px;
    margin: 8px 0 16px;
}
.uig-btn {
    padding: 7px 16px;
    border-radius: 20px;
    border: 1.5px solid #ffc1d6;
    background: linear-gradient(135deg, #fff0f5, #fff);
    color: #d63384;
    cursor: pointer;
    font-size: 12px;
    font-weight: 600;
    font-family: inherit;
    transition: all 0.3s cubic-bezier(0.4,0,0.2,1);
    position: relative;
    overflow: hidden;
}
.uig-btn:hover {
    background: linear-gradient(135deg, #ff6b9d, #ff1493);
    color: #fff;
    border-color: transparent;
    box-shadow: 0 6px 20px rgba(255,20,147,0.35);
    transform: translateY(-3px);
}
.uig-btn:active { transform: translateY(-1px); }
.uig-btn-clear { color: #bbb; border-color: #eee; background: #fafafa; }
.uig-btn-clear:hover {
    background: #f5f5f5; color: #999; border-color: #ddd;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05); transform: translateY(-2px);
}

/* ===== 折叠组 ===== */
.uig-details {
    margin: 8px 0;
    border: 1.5px solid #ffc1d6;
    border-radius: 12px;
    padding: 10px 14px;
    background: #fff;
    transition: all 0.3s cubic-bezier(0.4,0,0.2,1);
}
.uig-details:hover {
    box-shadow: 0 6px 24px rgba(255,107,157,0.15);
    border-color: #ff6b9d;
    transform: translateY(-1px);
}
.uig-details > summary {
    cursor: pointer;
    font-weight: 700;
    font-size: 13px;
    color: #d63384;
    padding: 4px 0;
    user-select: none;
    display: flex;
    align-items: center;
    gap: 8px;
    list-style: none;
}
.uig-details > summary::-webkit-details-marker { display: none; }
.uig-arrow {
    transition: transform 0.25s ease;
    display: inline-block;
    font-size: 11px;
    color: #ff6b9d;
}
.uig-details[open] .uig-arrow { transform: rotate(90deg); }
.uig-details[open] {
    background: linear-gradient(135deg, #fff, #fff8fb);
    box-shadow: 0 4px 16px rgba(255,107,157,0.1);
}

/* ===== 表单标签与提示 ===== */
.uig-label {
    font-weight: 600;
    margin-bottom: 4px;
    margin-top: 10px;
    font-size: 13px;
    color: #c2185b;
    display: flex;
    align-items: center;
    gap: 4px;
}
.uig-label::before { content: '\25B8'; color: #ff6b9d; font-size: 12px; }
.uig-hint {
    font-size: 11px;
    color: #c4889e;
    margin: 0 0 10px 10px;
    line-height: 1.6;
    white-space: pre-line;
    padding-left: 8px;
    border-left: 2px solid #ffe0ec;
}

/* ===== AntDesign 组件粉色覆盖 ===== */
.uig-container .ant-input {
    border-color: #ffc1d6 !important;
    border-radius: 8px !important;
    transition: all 0.3s !important;
}
.uig-container .ant-input:hover { border-color: #ff6b9d !important; }
.uig-container .ant-input:focus,
.uig-container .ant-input-focused {
    border-color: #ff6b9d !important;
    box-shadow: 0 0 0 2px rgba(255,107,157,0.2) !important;
}
.uig-container .ant-input-affix-wrapper {
    border-color: #ffc1d6 !important;
    border-radius: 8px !important;
    transition: all 0.3s !important;
}
.uig-container .ant-input-affix-wrapper:hover { border-color: #ff6b9d !important; }
.uig-container .ant-input-affix-wrapper-focused {
    border-color: #ff6b9d !important;
    box-shadow: 0 0 0 2px rgba(255,107,157,0.2) !important;
}
.uig-container .ant-radio-wrapper { color: #d63384; }
.uig-container .ant-radio-inner { border-color: #ffc1d6 !important; transition: all 0.3s !important; }
.uig-container .ant-radio:hover .ant-radio-inner { border-color: #ff6b9d !important; }
.uig-container .ant-radio-checked .ant-radio-inner {
    background-color: #ff6b9d !important;
    border-color: #ff6b9d !important;
}
.uig-container .ant-radio-checked .ant-radio-inner::after { background-color: #fff !important; }

/* ===== 页脚 ===== */
.uig-footer {
    text-align: center;
    font-size: 11px;
    color: #e8a0bf;
    margin-top: 20px;
    padding-top: 12px;
    border-top: 1px solid #ffe0ec;
}
";

    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        if (Configuration == null)
        {
            b.AddContent(0, "Configuration NULL");
            return;
        }

        int i = 0;

        // ===== 注入 CSS =====
        b.OpenElement(i++, "style");
        b.AddContent(i++, Css);
        b.CloseElement();

        // ===== 容器 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-container");

        // ===== 标题 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-title");
        b.AddContent(i++, "\U0001f338 \u901a\u7528\u751f\u56fe");
        b.CloseElement();

        // ===== 使用说明 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-alert");

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-alert-icon");
        b.AddContent(i++, "\U0001f4a1");
        b.CloseElement();

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-alert-body");

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-alert-title");
        b.AddContent(i++, "\u4f7f\u7528\u8bf4\u660e");
        b.CloseElement();

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-alert-desc");
        b.AddContent(i++,
            "\u8ba9 AI \u6839\u636e\u63cf\u8ff0\u751f\u6210\u6216\u4fee\u6539\u56fe\u7247\uff0cAI \u4f1a\u901a\u8fc7\u51fd\u6570\u5e8f\u53f7\u9009\u62e9\u4f7f\u7528\u54ea\u7ec4 API\n" +
            "\u4fee\u6539\u914d\u7f6e\u540e\u9700\u91cd\u65b0\u52a0\u8f7d\u6a21\u5757\uff08\u8bbe\u7f6e\u2192\u63d2\u4ef6\u2192\u5237\u65b0\uff09\n" +
            "\u56fe\u751f\u56fe\u9700\u8981 API \u670d\u52a1\u5546\u989d\u5916\u652f\u6301\uff0copenai \u683c\u5f0f\u4ec5\u90e8\u5206\u7b2c\u4e09\u65b9\u517c\u5bb9");
        b.CloseElement();

        b.CloseElement(); // alert-body
        b.CloseElement(); // alert

        // ===== 主 API 配置 =====
        AddSection(b, ref i, "\U0001f525 \u4e3b API \u914d\u7f6e\uff08\u7b2c 1 \u7ec4\uff09");

        AddStatusBadge(b, ref i, IsGroupConfigured(1));
        AddPresetButtons(b, ref i);

        BuildGroupFields(b, ref i,
            "Api1_",
            () => Configuration.Api1_Name, v => Configuration.Api1_Name = v,
            () => Configuration.Api1_Type, v => Configuration.Api1_Type = v,
            () => Configuration.Api1_Endpoint, v => Configuration.Api1_Endpoint = v,
            () => Configuration.Api1_ApiKey, v => Configuration.Api1_ApiKey = v,
            () => Configuration.Api1_Model, v => Configuration.Api1_Model = v,
            () => Configuration.Api1_DefaultSize, v => Configuration.Api1_DefaultSize = v);

        // ===== 备用 API =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:8px;");

        AddSection(b, ref i, "\U0001f4e6 \u5907\u7528 API\uff08\u53ef\u9009\uff0c\u70b9\u51fb\u5c55\u5f00\uff09");

        AddCollapsibleGroup(b, ref i, "API \u7ec4 2", IsGroupConfigured(2), () =>
            BuildGroupFields(b, ref i,
                "Api2_",
                () => Configuration.Api2_Name ?? "", v => Configuration.Api2_Name = v,
                () => Configuration.Api2_Type ?? "openai", v => Configuration.Api2_Type = v,
                () => Configuration.Api2_Endpoint ?? "", v => Configuration.Api2_Endpoint = v,
                () => Configuration.Api2_ApiKey ?? "", v => Configuration.Api2_ApiKey = v,
                () => Configuration.Api2_Model ?? "", v => Configuration.Api2_Model = v,
                () => Configuration.Api2_DefaultSize ?? "1024x1024", v => Configuration.Api2_DefaultSize = v));

        AddCollapsibleGroup(b, ref i, "API \u7ec4 3", IsGroupConfigured(3), () =>
            BuildGroupFields(b, ref i,
                "Api3_",
                () => Configuration.Api3_Name ?? "", v => Configuration.Api3_Name = v,
                () => Configuration.Api3_Type ?? "openai", v => Configuration.Api3_Type = v,
                () => Configuration.Api3_Endpoint ?? "", v => Configuration.Api3_Endpoint = v,
                () => Configuration.Api3_ApiKey ?? "", v => Configuration.Api3_ApiKey = v,
                () => Configuration.Api3_Model ?? "", v => Configuration.Api3_Model = v,
                () => Configuration.Api3_DefaultSize ?? "1024x1024", v => Configuration.Api3_DefaultSize = v));

        AddCollapsibleGroup(b, ref i, "API \u7ec4 4", IsGroupConfigured(4), () =>
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
        b.AddAttribute(i++, "style", "margin-top:8px;");

        AddSection(b, ref i, "\U0001f4be \u4fdd\u5b58\u8bbe\u7f6e");

        var currentSaveDir = string.IsNullOrWhiteSpace(Configuration.SaveDirectory)
            ? Path.Combine(AlifePath.StorageFolderPath, "Images", "Generated")
            : Configuration.SaveDirectory;

        AddInput(b, ref i, "\u56fe\u7247\u4fdd\u5b58\u76ee\u5f55", Configuration.SaveDirectory, v => Configuration.SaveDirectory = v);
        AddHint(b, ref i, $"\u5f53\u524d\u4fdd\u5b58\u4f4d\u7f6e\uff1a{currentSaveDir}\u3000\u7559\u7a7a\u5219\u4f7f\u7528\u9ed8\u8ba4\u76ee\u5f55");
        b.CloseElement();

        // ===== 页脚 =====
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "uig-footer");
        b.AddContent(i++, "Made with \U0001f496 by \u901a\u7528\u751f\u56fe\u63d2\u4ef6");
        b.CloseElement();

        // ===== 关闭容器 =====
        b.CloseElement();
    }

    // ======================================================================
    // 状态判断
    // ======================================================================

    bool IsGroupConfigured(int group)
    {
        return group switch
        {
            1 => !string.IsNullOrWhiteSpace(Configuration?.Api1_ApiKey),
            2 => !string.IsNullOrWhiteSpace(Configuration?.Api2_ApiKey),
            3 => !string.IsNullOrWhiteSpace(Configuration?.Api3_ApiKey),
            4 => !string.IsNullOrWhiteSpace(Configuration?.Api4_ApiKey),
            _ => false,
        };
    }

    // ======================================================================
    // 分区标题
    // ======================================================================

    void AddSection(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "uig-section");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    // ======================================================================
    // 状态徽标
    // ======================================================================

    void AddStatusBadge(RenderTreeBuilder b, ref int seq, bool configured)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style", "margin-bottom:12px;");

        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", configured ? "uig-badge-on" : "uig-badge-off");
        b.AddContent(seq++, configured ? "\u5df2\u914d\u7f6e" : "\u672a\u914d\u7f6e");
        b.CloseElement();

        b.CloseElement();
    }

    // ======================================================================
    // 预设按钮 (自定义粉色按钮)
    // ======================================================================

    void AddPresetButtons(RenderTreeBuilder b, ref int seq)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "uig-preset-row");

        PresetBtn(b, ref seq, "SiliconFlow", "openai",
            "https://api.siliconflow.cn/v1/images/generations", "black-forest-flux/1");
        PresetBtn(b, ref seq, "OpenAI", "openai",
            "https://api.openai.com/v1/images/generations", "dall-e-3");
        PresetBtn(b, ref seq, "\u901a\u4e49\u4e07\u76f8", "wan",
            "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation", "wan2.7-image");
        PresetBtn(b, ref seq, "Agnes", "agnes",
            "https://apihub.agnes-ai.com/v1/images/generations", "");
        PresetBtn(b, ref seq, "\u6e05\u7a7a", "", "", "");

        b.CloseElement();
    }

    void PresetBtn(RenderTreeBuilder b, ref int seq, string label, string type, string endpoint, string model)
    {
        var isClear = label == "\u6e05\u7a7a";

        b.OpenElement(seq++, "button");
        b.AddAttribute(seq++, "class", isClear ? "uig-btn uig-btn-clear" : "uig-btn");
        b.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () =>
        {
            Configuration.Api1_Type = type;
            Configuration.Api1_Endpoint = endpoint;
            Configuration.Api1_Model = model;
            if (isClear)
            {
                Configuration.Api1_Name = "\u9ed8\u8ba4";
                Configuration.Api1_Type = "agnes";
                Configuration.Api1_ApiKey = "";
                Configuration.Api1_DefaultSize = "1024x1024";
            }
        }));
        b.AddContent(seq++, isClear ? "\u21ba \u6e05\u7a7a" : label);
        b.CloseElement();
    }

    // ======================================================================
    // 折叠组
    // ======================================================================

    void AddCollapsibleGroup(RenderTreeBuilder b, ref int seq, string title, bool configured, Action renderContent)
    {
        b.OpenElement(seq++, "details");
        b.AddAttribute(seq++, "class", "uig-details");

        b.OpenElement(seq++, "summary");

        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", "uig-arrow");
        b.AddContent(seq++, "\u25B6");
        b.CloseElement();

        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "style", "flex:1;");
        b.AddContent(seq++, title);
        b.CloseElement();

        // 状态徽标
        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", configured ? "uig-badge-on" : "uig-badge-off");
        b.AddContent(seq++, configured ? "\u5df2\u914d\u7f6e" : "\u672a\u914d\u7f6e");
        b.CloseElement();

        b.CloseElement(); // summary

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

        AddInput(b, ref seq, "\u540d\u79f0", getName(), setName);
        AddHint(b, ref seq, "\u4ec5\u7528\u4e8e AI \u8bc6\u522b\u662f\u54ea\u7ec4 API\uff0c\u53ef\u968f\u4fbf\u586b");

        AddTypeSelector(b, ref seq, "\u63a5\u53e3\u7c7b\u578b", getType(), setType);
        if (isGroup1)
        {
            AddHint(b, ref seq,
                "\u6839\u636e API \u670d\u52a1\u5546\u9009\u62e9\uff1aOpenAI\uff08\u901a\u7528\u683c\u5f0f\uff09/ Agnes / \u901a\u4e49\u4e07\u76f8\n" +
                "\u56fe\u751f\u56fe\u63a8\u8350 Agnes \u6216 Wan \u683c\u5f0f\uff0cOpenAI \u683c\u5f0f\u4ec5\u90e8\u5206\u7b2c\u4e09\u65b9\u517c\u5bb9");
        }
        else
        {
            AddHint(b, ref seq, "OpenAI / Agnes / Wan \u4e09\u79cd\u683c\u5f0f");
        }

        AddInput(b, ref seq, "API Endpoint", getEndpoint(), setEndpoint);
        AddHint(b, ref seq, "\u5b8c\u6574\u7684 API \u63a5\u53e3\u5730\u5740\uff0c\u6ce8\u610f\u533a\u5206\u6587\u751f\u56fe\u548c\u56fe\u751f\u56fe\u7684\u63a5\u53e3");

        AddPassword(b, ref seq, "API Key", getKey(), setKey);
        AddHint(b, ref seq, "API \u5bc6\u94a5\uff0c\u4e0d\u4f1a\u660e\u6587\u663e\u793a");

        AddInput(b, ref seq, "\u6a21\u578b\u540d\u79f0", getModel(), setModel);
        AddHint(b, ref seq, "\u4f8b\u5982\uff1adall-e-3 / black-forest-flux/1 / wanx2.1-t2i-turbo");

        AddInput(b, ref seq, "\u9ed8\u8ba4\u5c3a\u5bf8", getSize(), setSize);
        AddHint(b, ref seq, "\u683c\u5f0f\uff1a\u5bbdx\u9ad8\uff0c\u5982 1024x1024\u3002\u8c03\u7528\u51fd\u6570\u65f6\u53ef\u5355\u72ec\u6307\u5b9a\u5bbd\u9ad8\u8986\u76d6\u6b64\u503c");
    }

    // ======================================================================
    // 接口类型选择器 (RadioGroup)
    // ======================================================================

    void AddTypeSelector(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);

        b.OpenComponent<RadioGroup<string>>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged", EventCallback.Factory.Create<string>(this, setter));
        b.AddAttribute(seq++, "ChildContent", (RenderFragment)(child =>
        {
            child.OpenComponent<Radio<string>>(0);
            child.AddAttribute(1, "Value", "openai");
            child.AddAttribute(2, "ChildContent", (RenderFragment)(c => c.AddContent(0, "OpenAI")));
            child.CloseComponent();

            child.OpenComponent<Radio<string>>(3);
            child.AddAttribute(4, "Value", "agnes");
            child.AddAttribute(5, "ChildContent", (RenderFragment)(c => c.AddContent(0, "Agnes")));
            child.CloseComponent();

            child.OpenComponent<Radio<string>>(6);
            child.AddAttribute(7, "Value", "wan");
            child.AddAttribute(8, "ChildContent", (RenderFragment)(c => c.AddContent(0, "\u901a\u4e49\u4e07\u76f8")));
            child.CloseComponent();
        }));
        b.CloseComponent();
    }

    // ======================================================================
    // 工具函数
    // ======================================================================

    void AddHint(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "uig-hint");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddLabel(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "uig-label");
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
}
