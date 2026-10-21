using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;

internal sealed class UnscrewFlow1 : ScrewFlowBase
{
    public UnscrewFlow1(IFlowRuntimeService runtime, IFlowIoService io, IProductService products,
        IScrewCurveProvider curves, IDeviceFactory devices, ILogger<UnscrewFlow1> logger)
        : base(runtime, io, products, curves, devices, logger)
    {
    }

    public override string FlowName => "UNSCREW1";
}

internal sealed class UnscrewFlow2 : ScrewFlowBase
{
    public UnscrewFlow2(IFlowRuntimeService runtime, IFlowIoService io, IProductService products,
        IScrewCurveProvider curves, IDeviceFactory devices, ILogger<UnscrewFlow2> logger)
        : base(runtime, io, products, curves, devices, logger)
    {
    }

    public override string FlowName => "UNSCREW2";
}
