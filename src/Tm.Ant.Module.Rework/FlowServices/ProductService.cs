using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.FlowServices;
public interface IProductService : ISingleton
{
    Task<bool> AddFlowAsync(string productCode,string caves,string locations);
    Task<bool> UpdateFlowAsync(string productCode,int cave,int result);
    Task<FlowMain> GetFlowAsync(string productCode);
    Task<FlowMain> OfflineFlowAsync(string productCode);
}
internal class ProductService : IProductService
{
    private readonly IRepositoryFactory _repositoryFactory;
    public ProductService(IRepositoryFactory repositoryFactory)
    {
        _repositoryFactory=repositoryFactory;
    }
    public async Task<bool> AddFlowAsync(string productCode, string caves,string locations)
    {
       using var repo= _repositoryFactory.CreateRepository<FlowMain>();
        var result = await repo.InsertAsync(new FlowMain() 
        {
             ProductCode=productCode,
             UnScrewCaves=caves,
             Locations=locations
        });
        return result > 0;
    }

    public async Task<FlowMain> GetFlowAsync(string productCode)
    {
        using var repo = _repositoryFactory.CreateRepository<FlowMain>();
        var result = await repo.FetchLatestAsync(x => x.ProductCode == productCode&&x.IsDeleted==0);
        return result;
    }
    public async Task<bool> UpdateFlowAsync(string productCode, int cave, int result)
    {
        using var repo = _repositoryFactory.CreateRepository<FlowMain>();
        var entity = await repo.FetchLatestAsync(x => x.ProductCode == productCode && x.IsDeleted == 0);
        if (entity == null) return false;
        var resultJsonItems = new List<ResultJsonItem>();
        if (!string.IsNullOrEmpty(entity.ResultJson))
        {
            try
            {
                var existResult=JsonHelper.DeserializeObject<List<ResultJsonItem>>(entity.ResultJson);
                resultJsonItems.AddRange(existResult);
            }
            catch { }
        }
        resultJsonItems.Add(new ResultJsonItem() { Cave=cave,Result=result==1?"OK":"NG" });
        entity.ResultJson=JsonHelper.SerializeObject(resultJsonItems);
        var rows = await repo.UpdateAsync(entity);
        return rows > 0;
    }
    public async Task<FlowMain> OfflineFlowAsync(string productCode)
    {
        using var repo = _repositoryFactory.CreateRepository<FlowMain>();
        var flowMain = await repo.FetchLatestAsync(x => x.ProductCode == productCode && x.IsDeleted == 0);
        if (flowMain == null)
        {
            return null;
        }

        flowMain.IsDeleted = 1;
        var rows = await repo.UpdateAsync(flowMain);
        if (rows <= 0)
        {
            throw new InvalidOperationException($"产品 {productCode} 下线失败，主表软删除未成功。");
        }

        return flowMain;
    }
}
