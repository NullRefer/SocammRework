using Tm.Ant.Framework.Models.Events;

namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// SFC上传前取得的一件产品最终结果（true为OK），时间表示结果取得时刻。
/// </summary>
public sealed record ScrewDisplaySnapshot(string ProductCode, DateTime TriggeredAt, DateTime UnloadTime,
    IReadOnlyDictionary<int, bool> CaveResults);

/// <summary>
/// 通知页面刷新当前最新螺丝结果，不表示SFC上传或软下线成功。
/// </summary>
public sealed class ScrewDisplayChangedEvent : PubSubEvent<ScrewDisplayChangedEvent>
{
    public ScrewDisplaySnapshot Snapshot { get; init; }
}

/// <summary>
/// 图片叠加标记，坐标使用PCB原图空间，编号从1开始显示。
/// </summary>
public sealed record ScrewDisplayMarker(int Index, double X, double Y, string Result, string Label);
