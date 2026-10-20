namespace Tm.Ant.Module.Rework.Models.Pto;

/// <summary>
/// 两台拆螺丝机共用的协议：扫码下发、两套只读曲线关联、最终结果上传。
/// </summary>
public sealed class ScrewAddress
{
    public ScanGroup Scan { get; } = new();
    public CurveGroup Curve1 { get; } =
        new("D101", "D132", [new(1, "D133", "D169"), new(3, "D134", "D170")]);
    public CurveGroup Curve2 { get; } = new("D135", "D166", [new(2, "D167", "D171")]);
    public ResultGroup Result { get; } = new("D168", "D135", "D166");

    public sealed class ScanGroup
    {
        public string Trigger { get; } = "D100";
        public string Feedback { get; } = "D200";
        public string ProductCode { get; } = "D201";
        public string Updated { get; } = "D232";
        public string RequiredCaves { get; } = "D233";
    }

    public sealed record CurveGroup(string ProductCode, string Cave, IReadOnlyList<ScrewItem> Screws);
    public sealed record ScrewItem(int Number, string Program, string ToolNo);
    public sealed record ResultGroup(string Trigger, string ProductCode, string Cave);
}
