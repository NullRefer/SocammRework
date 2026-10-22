using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

public sealed class ReworkHomeLayout : HomeLayout
{
    private readonly IMachineConfigService _machineConfigService;

    public ReworkHomeLayout(IServiceProvider services, IMachineConfigService machineConfigService)
        : base(services)
    {
        _machineConfigService = machineConfigService;
    }

    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        if (_machineConfigService.GetCurrentMachine()?.FlowName == "ULD")
        {
            RegisterLeftRightLayout<ScrewDisplay>("螺丝显示");
        }

        RegisterLeftRightLayout<ManualPass>("手动过站");
        RegisterLeftRightLayout<Report>("产品报表");
    }
}
