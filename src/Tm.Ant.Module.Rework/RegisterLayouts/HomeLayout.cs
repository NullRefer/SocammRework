using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

public sealed class ReworkHomeLayout : HomeLayout
{
    private readonly IMachineConfigService _machines;

    public ReworkHomeLayout(IServiceProvider services, IMachineConfigService machines)
        : base(services)
    {
        _machines = machines;
    }

    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        if (_machines.GetCurrentMachine()?.FlowName == FlowType.ULD.ToString())
        {
            RegisterLeftRightLayout<PCBResultDisplay>("拆除结果");
        }

        RegisterLeftRightLayout<ManualPass>("手动过站");
        RegisterLeftRightLayout<Report>("产品报表");
    }
}
