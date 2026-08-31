using Tm.Ant.CommonBLL.Model;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework;

internal class RewokModule : AbstractTmModule
{
    public override void Configure(IServiceCollection services, IConfiguration configuration)
    { 
        services.RegisterUIServices(ThisAssembly);

        //上料机才注册
        if (configuration.GetCurrentMachine()?.FlowName == FlowType.LD.ToString())
        {
            //vk主页面添加 开关、信息提示
            services.RegisterToRegion<UnitShield>(RegionNames.MainShieldRegion);
        }
    }
}
