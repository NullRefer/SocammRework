using Tm.Ant.CommonBLL.Interfaces;
using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.Provider;
public interface IShieldProvider:ISingleton
{
    Task SaveAsync(ShieldModel shield);
    Task<ShieldModel> GetShieldAsync();
}
public class ShieldProvider: IShieldProvider
{
    private readonly IDictService _dictService;
    public ShieldProvider(IDictService dictService)
    {
        _dictService= dictService;
    }

    public async Task<ShieldModel> GetShieldAsync()
    {
        var shield = await _dictService.GetAsync<ShieldModel>(FlowDictKeys.Shield);
        if (shield == null)
            shield = new ShieldModel();
        return shield;
    }

    public async Task SaveAsync(ShieldModel shield)
    {
        await _dictService.AddOrUpdateKeyAsync(FlowDictKeys.Shield, JsonHelper.SerializeObject(shield));
    }
}
