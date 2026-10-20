using Tm.Ant.Framework.Models.Events;

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
/// 曲线文件保存完成后发布的展示事件，不写业务数据库。
/// </summary>
public sealed class ScrewCurveEvent : PubSubEvent<ScrewCurveEvent>
{
    public string ProductCode { get; set; } = "";
    public int Cave { get; set; }
    public ScrewCurveModel ScrewCurve { get; set; } = new();
}
