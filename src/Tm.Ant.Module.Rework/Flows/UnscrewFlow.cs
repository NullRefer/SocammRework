using Tm.Ant.Module.Rework.Interfaces.Flows;

namespace Tm.Ant.Module.Rework.Flows;

internal sealed class UnscrewFlow1(IFlowRuntimeService runtime, IScrewService service,
    IDeviceFactory devices, ILogger<UnscrewFlow1> logger) : ScrewFlowBase(runtime, service, devices, logger)
{
    public override string FlowName => "UNSCREW1";
}

internal sealed class UnscrewFlow2(IFlowRuntimeService runtime, IScrewService service,
    IDeviceFactory devices, ILogger<UnscrewFlow2> logger) : ScrewFlowBase(runtime, service, devices, logger)
{
    public override string FlowName => "UNSCREW2";
}
