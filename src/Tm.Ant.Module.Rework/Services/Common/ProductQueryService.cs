using Tm.Ant.Module.Rework.Entities;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class ProductQueryService : IProductQueryService
{
    private readonly IRepositoryFactory _repositories;

    public ProductQueryService(IRepositoryFactory repositories)
    {
        _repositories = repositories;
    }

    public async Task<PagedResultDto<FlowMain>> QueryAsync(ProductQueryRequest request, CancellationToken token)
    {
        using var repo = _repositories.CreateRepository<FlowMain>();
        var query = repo.GetAll()
            .Where(item => item.CreateTime >= request.StartTime && item.CreateTime < request.EndTime);
        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            query = query.Where(item => item.ProductCode.Contains(request.ProductCode));
        }

        return await query.OrderByDescending(item => item.Id)
            .PagedQueryAsync(request.PageIndex, request.PageSize, token);
    }
}
