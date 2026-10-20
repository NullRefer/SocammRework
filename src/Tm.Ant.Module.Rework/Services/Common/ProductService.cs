using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class ProductService(IRepositoryFactory repositories, ILogger<ProductService> logger) : IProductService
{
    public async Task<OperationResult<FlowMain>> AddFlowAsync(string code, short[] caves, string locations,
        CancellationToken token)
    {
        if (caves.Length != 8 || caves.Any(value => value is not (0 or 1)))
        {
            return OperationResult.Error<FlowMain>($"创建产品({code})失败：需拆标记要求8个0/1。");
        }

        using var repo = repositories.CreateRepository<FlowMain>();
        var entity = new FlowMain
        {
            ProductCode = code,
            UnScrewCaves = string.Join(',', caves),
            Locations = locations ?? "",
            ResultJson = "[]"
        };
        if (await repo.InsertAsync(entity, token) <= 0)
        {
            return OperationResult.Error<FlowMain>($"创建产品({code})失败：数据库未新增记录。");
        }

        logger.LogInformation("新增生产记录，ID({Id})，产品({Code})，需拆({Caves})，位置({Locations})",
            entity.Id, code, entity.UnScrewCaves, entity.Locations);
        return OperationResult.Success(entity);
    }

    public async Task<FlowMain> GetFlowAsync(string code, CancellationToken token)
    {
        using var repo = repositories.CreateRepository<FlowMain>();
        return await repo.FetchLatestAsync(item => item.ProductCode == code && item.IsDeleted == 0, token);
    }

    public async Task<OperationResult> UpdateFlowAsync(string code, int cave, short result, CancellationToken token)
    {
        if (cave is < 1 or > 8 || result is not (1 or 2))
        {
            return OperationResult.Error($"保存拆除结果校验失败，产品({code})，穴位({cave})，结果({result})：要求穴位1~8、结果1/2。");
        }

        using var repo = repositories.CreateRepository<FlowMain>();
        var entity = await repo.FetchLatestAsync(item => item.ProductCode == code && item.IsDeleted == 0, token);
        if (entity == null)
        {
            return OperationResult.Error($"保存拆除结果失败，产品({code})：未找到在线记录。");
        }

        var results = entity.GetResults();
        results.Add(new ResultJsonItem { Cave = cave, Result = result == 1 ? "OK" : "NG" });
        entity.ResultJson = JsonConvert.SerializeObject(results);
        if (await repo.UpdateAsync(entity, token) <= 0)
        {
            return OperationResult.Error($"保存拆除结果失败，产品({code})，穴位({cave})：数据库未更新记录。");
        }

        logger.LogInformation("更新生产记录，ID({Id})，产品({Code})，ResultJson({Results})",
            entity.Id, code, entity.ResultJson);
        return OperationResult.Success();
    }

    public async Task<OperationResult> OfflineFlowAsync(FlowMain entity, CancellationToken token)
    {
        using var repo = repositories.CreateRepository<FlowMain>();
        entity.IsDeleted = 1;
        if (await repo.UpdateAsync(entity, token) <= 0)
        {
            return OperationResult.Error($"产品({entity.ProductCode})下线失败：主表软删除未成功。");
        }

        logger.LogInformation("生产记录下线，ID({Id})，产品({Code})，IsDeleted(1)", entity.Id, entity.ProductCode);
        return OperationResult.Success();
    }
}
