namespace Tm.Ant.Module.Rework.Models.SFC;

/// <summary>
/// 普通过站接口 STATION_RECORD 请求，合格产品上传 PASS。
/// </summary>
public sealed class StationRecordRequest
{
    [JsonProperty("SN")]
    public string Sn { get; set; }

    [JsonProperty("RESULT")]
    public string Result { get; set; }

    [JsonProperty("EMP")]
    public string Emp { get; set; }

    [JsonProperty("MACHINE_CODE")]
    public string MachineCode { get; set; }

    [JsonProperty("ERROR_CODE")]
    public string ErrorCode { get; set; }
}

/// <summary>
/// 普通过站接口响应，rc 为 0 表示过站成功。
/// </summary>
public sealed class StationRecordResponse
{
    [JsonProperty("rc")]
    public int Rc { get; set; }

    [JsonProperty("Error_Description")]
    public string Result { get; set; }
}
