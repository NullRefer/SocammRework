using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

public sealed class ReworkHomeLayout(IServiceProvider services, IMachineConfigService machines) : HomeLayout(services)
{
    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        if (machines.GetCurrentMachine()?.FlowName == FlowType.ULD.ToString())
        {
            RegisterLeftRightLayout<PCBResultDisplay>("拆除结果");
        }

        RegisterLeftRightLayout<ManualPass>("手动过站");
    }
}
