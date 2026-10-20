namespace Tm.Ant.Module.Rework.Models.Pto;

/// <summary>
/// 上料扫码、建档交互地址。
/// </summary>
public sealed class LoadAddress
{
    public string Trigger { get; } = "D100";
    public string Feedback { get; } = "D200";
    public string ProductCode { get; } = "D201";
}
