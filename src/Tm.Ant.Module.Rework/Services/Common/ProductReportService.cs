using Microsoft.EntityFrameworkCore;

using Tm.Ant.Module.Rework.Entities;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Services.Common;

/// <summary>
/// 产品报表共用筛选与DTO投影，提供分页查询和全部导出数据。
/// </summary>
internal sealed class ProductReportService : IProductReportService
{
    private readonly IRepositoryFactory _repositoryFactory;

    public ProductReportService(IRepositoryFactory repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
    }

    public async Task<PagedResultDto<ProductReportRowDto>> QueryAsync(ProductReportQueryRequest request,
        CancellationToken token)
    {
        using var repo = _repositoryFactory.CreateRepository<FlowMain>();
        return await CreateQuery(repo.GetAll(), request).PagedQueryAsync(request.PageIndex, request.PageSize, token);
    }

    public async Task<List<ProductReportRowDto>> GetExportDataAsync(ProductReportQueryRequest request,
        CancellationToken token)
    {
        using var repo = _repositoryFactory.CreateRepository<FlowMain>();
        return await CreateQuery(repo.GetAll(), request).ToListAsync(token);
    }

    private static IQueryable<ProductReportRowDto> CreateQuery(IQueryable<FlowMain> source,
        ProductReportQueryRequest request)
    {
        var predicate = ExpressionExtension.True<FlowMain>()
            .AndContains(item => item.ProductCode, request.ProductCode?.Trim())
            .AndGreaterThanOrEqual(item => item.CreateTime, request.StartTime)
            .AndLessThan(item => item.CreateTime, request.EndTime);

        return source.Where(predicate).OrderByDescending(item => item.Id).Select(item => new ProductReportRowDto
        {
            Id = item.Id,
            ProductCode = item.ProductCode,
            UnScrewCaves = item.UnScrewCaves,
            Locations = item.Locations,
            ResultJson = item.ResultJson,
            Status = item.IsDeleted == 1 ? "已下线" : "在线",
            CreateTime = item.CreateTime,
            UpdateTime = item.UpdateTime
        });
    }
}
