using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework;

/// <summary>
/// 重工线业务模块，普通服务、Flow和设备均由框架扫描。
/// </summary>
internal sealed class ReworkModule : AbstractTmModule
{
    public override void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterUIServices(ThisAssembly);
        if (configuration.GetLocalConfigOption().FlowName == "LD")
        {
            services.RegisterToRegion<UnitShield>(RegionNames.MainShieldRegion);
        }
    }
}
