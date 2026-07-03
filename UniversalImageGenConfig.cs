namespace Alife.Plugin.ImageGen.Universal;

public class UniversalImageGenConfig
{
    // === API 组 1（ApiKey 非空则启用）===
    public string Api1_Name { get; set; } = "默认";
    public string Api1_Type { get; set; } = "agnes";
    public string Api1_Endpoint { get; set; } = "";
    public string Api1_ApiKey { get; set; } = "";
    public string Api1_Model { get; set; } = "";
    public string Api1_DefaultSize { get; set; } = "1024x1024";

    // === API 组 2（可选）===
    public string? Api2_Name { get; set; }
    public string? Api2_Type { get; set; }
    public string? Api2_Endpoint { get; set; }
    public string? Api2_ApiKey { get; set; }
    public string? Api2_Model { get; set; }
    public string? Api2_DefaultSize { get; set; }

    // === API 组 3（可选）===
    public string? Api3_Name { get; set; }
    public string? Api3_Type { get; set; }
    public string? Api3_Endpoint { get; set; }
    public string? Api3_ApiKey { get; set; }
    public string? Api3_Model { get; set; }
    public string? Api3_DefaultSize { get; set; }

    // === API 组 4（可选）===
    public string? Api4_Name { get; set; }
    public string? Api4_Type { get; set; }
    public string? Api4_Endpoint { get; set; }
    public string? Api4_ApiKey { get; set; }
    public string? Api4_Model { get; set; }
    public string? Api4_DefaultSize { get; set; }

    // === 保存设置 ===
    public string SaveDirectory { get; set; } = "";
}
