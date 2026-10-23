namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 查询产品主表分页及导出数据，包含已软下线记录。
/// </summary>
public interface IProductReportService : ISingleton
{
    /// <summary>
    /// 按筛选条件返回产品报表DTO分页，仓储释放前完成物化。
    /// </summary>
    Task<PagedResultDto<ProductReportRowDto>> QueryAsync(ProductReportQueryRequest request, CancellationToken token);

    /// <summary>
    /// 返回全部筛选结果供页面导出，不受请求页码和页大小限制。
    /// </summary>
    Task<List<ProductReportRowDto>> GetExportDataAsync(ProductReportQueryRequest request, CancellationToken token);
}
