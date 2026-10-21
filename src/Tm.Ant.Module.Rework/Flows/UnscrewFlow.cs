using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Flows;

internal sealed class UnscrewFlow1 : ScrewFlowBase
{
    public UnscrewFlow1(IFlowRuntimeService runtime, IFlowIoService io, IProductService productService,
        ILogger<UnscrewFlow1> logger)
        : base(runtime, io, productService, logger)
    {
    }

    public override string FlowName => "UNSCREW1";
}

internal sealed class UnscrewFlow2 : ScrewFlowBase
{
    public UnscrewFlow2(IFlowRuntimeService runtime, IFlowIoService io, IProductService productService,
        ILogger<UnscrewFlow2> logger)
        : base(runtime, io, productService, logger)
    {
    }

    public override string FlowName => "UNSCREW2";
}
