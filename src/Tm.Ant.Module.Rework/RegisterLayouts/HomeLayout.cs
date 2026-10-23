using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

public sealed class ReworkHomeLayout : HomeLayout
{
    public ReworkHomeLayout(IServiceProvider services) : base(services)
    {
    }

    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        if (_serviceProvider.GetRequiredService<IConfiguration>().GetLocalConfigOption().FlowName == "ULD")
        {
            RegisterLeftRightLayout<ScrewDisplay>("螺丝显示");
        }

        RegisterLeftRightLayout<ManualPass>("手动过站");
        RegisterLeftRightLayout<Report>("产品报表");
    }
}
