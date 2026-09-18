using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;
public class ReworkHomeLayout : HomeLayout
{
    private readonly IConfiguration _configuration;
    public ReworkHomeLayout(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
    {
        _configuration = configuration;
    }
    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();

        var flowName = _configuration.GetCurrentMachine()?.FlowName;
        if(flowName==FlowType.ULD.ToString())
        {
            RegisterLeftRightLayout<PCBResultDisplay>("拆螺丝结果");
        }
        RegisterLeftRightLayout<ManualPass>("手动过站");
    }
}
