namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 本地曲线文件的数据内容，保留旧文件的字段名称。
/// </summary>
public sealed class ScrewCurveModel
{
    public int ScrewId { get; set; }
    public int ScrewNum { get; set; }
    public int ProgramNo { get; set; }
    public List<float> AngleList { get; set; } = new();
    public List<float> TorqueList { get; set; } = new();
}

/// <summary>
/// 曲线本地文件的产品、穴位与采样数据。
/// </summary>
public sealed class ScrewCurveData
{
    public string ProductCode { get; set; } = "";
    public int Cave { get; set; }
    public ScrewCurveModel ScrewCurve { get; set; } = new();
}
