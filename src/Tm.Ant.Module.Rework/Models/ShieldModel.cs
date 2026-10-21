namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 共享字典 Shield 的 SFC 开关。
/// </summary>
[DictJsonConfig("Shield", "重工线屏蔽开关")]
public sealed class ShieldModel
{
    public bool Sfc { get; set; }
}
