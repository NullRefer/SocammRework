using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 主表建档、在线查询、穴位结果保存和软下线。
/// </summary>
public interface IProductService : ISingleton
{
    /// <summary>
    /// 保存上料产品、八穴需拆标记与SFC原始位置，返回新增主表记录。
    /// </summary>
    Task<OperationResult<FlowMain>> AddFlowAsync(string code, short[] caves, string locations, CancellationToken token);
    /// <summary>
    /// 按产品码查询最新未软删除的主表记录，未找到返回null。
    /// </summary>
    Task<FlowMain> GetFlowAsync(string code, CancellationToken token);
    /// <summary>
    /// 校验穴位1~8和质量1/2，向最新在线记录追加穴位结果并记录数据库变更。
    /// </summary>
    Task<OperationResult> UpdateFlowAsync(string code, int cave, short result, CancellationToken token);
    /// <summary>
    /// 按已取得记录的ID软删除本次生产周期，不转移历史表。
    /// </summary>
    Task<OperationResult> OfflineFlowAsync(FlowMain entity, CancellationToken token);
}
