namespace Tm.Ant.Module.Rework.Models;
internal class ReworkOption
{
    public IpAndPort Scanner { get; set; } = new();
    public IpAndPort FirstScrew { get; set; } = new();
    public IpAndPort SecondScrew { get; set; }=new();

    public SfcConfigModel Sfc { get;set; }=new();
}


public class IpAndPort
{
    public string Ip { get; set; }
    public int Port { get; set; }
}

public class SfcConfigModel
{
    public string Emp { get; set; }
    public string MachineCode { get; set; }
    public string RecordUrl { get; set; }
    public string ReworkUrl { get; set; }
    public string Group { get; set; }
}
