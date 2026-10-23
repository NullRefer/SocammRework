namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 保存进程内最新螺丝显示快照并发布变更通知，不保存历史数据。
/// </summary>
public interface IScrewDisplayService : ISingleton
{
    /// <summary>
    /// 当前进程内最新结果，尚未下料时为空。
    /// </summary>
    ScrewDisplaySnapshot Latest { get; }

    /// <summary>
    /// 复制各穴位最终结果（true为OK），在SFC上传前更新最新快照并通知页面。
    /// </summary>
    void Publish(string productCode, DateTime time, IReadOnlyDictionary<int, bool> caveResults);
}
