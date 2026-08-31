using Newtonsoft.Json;

namespace Tm.Ant.Module.Rework.Models;
public abstract class RequestBase
{
    [JsonProperty("SN")]
    public string Sn { get; set; }
    /// <summary>
} 
public class RecoredRequest : RequestBase
{
    [JsonProperty("RESULT")]
    public string Result { get; set; }
    [JsonProperty("EMP")]
    public string Emp { get; set; }
    [JsonProperty("MACHINE_CODE")]
    public string MachineCode { get; set; }
    [JsonProperty("ERROR_CODE")]
    public string ErrorCode { get; set; }
}

public class NvdReworkRequest : RequestBase
{

}
public class NvdReworkResponse: NvdSfcResponse
{
    /// <summary>
    /// 多个逗号分隔
    /// </summary>
    [JsonProperty("DEKIT_LOCATION")]
    public string Locations { get; set; }
    [JsonProperty("GROUP")]
    public string Group { get; set; }
    /// <summary>
    /// PCB板是否匹配
    /// </summary>
    [JsonIgnore]
    public bool PcbNg { get; set; }
    public short[] DoCaves { get; set; } = new short[] {0,0,0,0,0,0,0,0 };
}

public class NvdSfcResponse
{
    public bool IsSuccess => Rc == 0;
    [JsonProperty("rc")]
    public int Rc { get; set; }
    [JsonProperty("Error Description")]
    public string Result { get; set; }
}
