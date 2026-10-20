using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 分页查询主表，包含已下线记录。
/// </summary>
public interface IProductQueryService : ISingleton
{
    /// <summary>
    /// 在仓储释放前完成主表分页查询，包含已软下线记录。
    /// </summary>
    Task<PagedResultDto<FlowMain>> QueryAsync(DateTime start, DateTime end, string code, int page, int size,
        CancellationToken token);
}
