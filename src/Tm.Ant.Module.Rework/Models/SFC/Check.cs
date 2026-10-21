namespace Tm.Ant.Module.Rework.Models.SFC;

/// <summary>
/// 上料确认接口 GET_DEKIT_LOCATION 请求。
/// </summary>
public sealed class CheckRequest
{
    [JsonProperty("SN")]
    public string Sn { get; set; }
}

/// <summary>
/// 上料确认接口响应，仅提供当前站点和本产品需拆位置。
/// </summary>
public sealed class CheckResponse
{
    /// <summary>
    /// 逗号分隔的需拆位置，例如 J168,J170,L168,L170。
    /// </summary>
    [JsonProperty("DEKIT_LOCATION")]
    public string Locations { get; set; }

    [JsonProperty("GROUP")]
    public string Group { get; set; }
}
