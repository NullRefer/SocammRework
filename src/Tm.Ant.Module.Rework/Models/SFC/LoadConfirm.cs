namespace Tm.Ant.Module.Rework.Models.SFC;

/// <summary>
/// 上料确认接口 GET_DEKIT_LOCATION 请求。
/// </summary>
public sealed class LoadConfirmRequest
{
    [JsonProperty("SN")]
    public string Sn { get; set; }
}

/// <summary>
/// 上料确认接口响应，提供当前站点和本产品需拆位置。
/// </summary>
public sealed class LoadConfirmResponse
{
    public bool IsSuccess => Rc == 0;

    [JsonProperty("rc", Required = Required.Always)]
    public int Rc { get; set; }

    [JsonProperty("Error Description")]
    public string Result { get; set; }

    /// <summary>
    /// 逗号分隔的需拆位置，例如 J168,J170,L168,L170。
    /// </summary>
    [JsonProperty("DEKIT_LOCATION")]
    public string Locations { get; set; }

    [JsonProperty("GROUP")]
    public string Group { get; set; }

    /// <summary>
    /// 软件由需拆位置解析的八穴标记，不属于 SFC 响应字段。
    /// </summary>
    public short[] DoCaves { get; set; } = new short[] { 0, 0, 0, 0, 0, 0, 0, 0 };
}
