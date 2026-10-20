using Tm.Ant.Module.Rework.Entities;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class ProductQueryService(IRepositoryFactory repositories) : IProductQueryService
{
    public async Task<PagedResultDto<FlowMain>> QueryAsync(DateTime start, DateTime end, string code, int page,
        int size, CancellationToken token)
    {
        using var repo = repositories.CreateRepository<FlowMain>();
        var query = repo.GetAll().Where(item => item.CreateTime >= start && item.CreateTime < end);
        if (!string.IsNullOrWhiteSpace(code))
        {
            query = query.Where(item => item.ProductCode.Contains(code));
        }

        return await query.OrderByDescending(item => item.Id).PagedQueryAsync(page, size, token);
    }
}
