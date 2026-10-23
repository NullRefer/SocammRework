namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 查询与导出产品主表报表，包含已软下线记录。
/// </summary>
public interface IProductReportService : ISingleton
{
    /// <summary>
    /// 按筛选条件返回产品报表DTO分页，仓储释放前完成物化。
    /// </summary>
    Task<PagedResultDto<ProductReportRowDto>> QueryAsync(ProductReportQueryRequest request, CancellationToken token);

    /// <summary>
    /// 导出全部筛选结果至Excel并返回记录数，不受请求页码和页大小限制。
    /// </summary>
    Task<int> ExportAsync(ProductReportQueryRequest request, string filePath, CancellationToken token);
}
