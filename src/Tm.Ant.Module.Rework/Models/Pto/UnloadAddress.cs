namespace Tm.Ant.Module.Rework.Models.Pto;

/// <summary>
/// 下料扫码、质量判定与软下线交互地址。
/// </summary>
public sealed class UnloadAddress
{
    public string Trigger { get; } = "D101";
    public string Feedback { get; } = "D201";
    public string ProductCode { get; } = "D232";
}
