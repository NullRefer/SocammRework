namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 产品主表分页查询条件，包含软下线记录。
/// </summary>
/// <param name="PageIndex">页码，从1开始。</param>
/// <param name="PageSize">每页记录数。</param>
/// <param name="ProductCode">产品码包含匹配，为空时不筛选。</param>
/// <param name="StartTime">创建时间起点，包含。</param>
/// <param name="EndTime">创建时间终点，不包含。</param>
public sealed record ProductQueryRequest(
    int PageIndex,
    int PageSize,
    string ProductCode,
    DateTime StartTime,
    DateTime EndTime) : PagedQueryRequestBase(PageIndex, PageSize);
