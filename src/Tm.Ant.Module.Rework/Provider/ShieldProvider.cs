namespace Tm.Ant.Module.Rework.Provider;

/// <summary>
/// 读取和保存共享屏蔽配置。
/// </summary>
public interface IShieldProvider : ISingleton
{
    Task<ShieldModel> GetShieldAsync(CancellationToken token = default);
    Task SaveAsync(ShieldModel shield, CancellationToken token = default);
}

internal sealed class ShieldProvider : IShieldProvider
{
    private readonly IConfigService _configs;

    public ShieldProvider(IConfigService configs)
    {
        _configs = configs;
    }

    public async Task<ShieldModel> GetShieldAsync(CancellationToken token = default)
    {
        return await _configs.GetAsync<ShieldModel>(token);
    }

    public async Task SaveAsync(ShieldModel shield, CancellationToken token = default)
    {
        await _configs.SaveAsync(shield, token);
    }
}
