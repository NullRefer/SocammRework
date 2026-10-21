namespace Tm.Ant.Module.Rework.Models.SFC;

/// <summary>
/// 需拆查询接口 GET_DEKIT_LOCATION 请求。
/// </summary>
public sealed class CheckRequest
{
    [JsonProperty("SN")]
    public string Sn { get; set; }
}

/// <summary>
/// 需拆查询接口响应，提供当前站点和本产品需拆位置。
/// </summary>
public sealed class CheckResponse
{
    [JsonProperty("rc")]
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
}
