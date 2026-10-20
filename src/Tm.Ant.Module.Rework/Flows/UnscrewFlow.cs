using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;

internal class UnscrewFlow1 : ScrewFlowBase
{
    public UnscrewFlow1(
        IConfiguration configuration,
        ITmSocketFactory tmSocketFactory,
        ITmLogger tmLogger,
        IPlcFactory plcFactory,
        IPlcAddProvider plcAddProvider,
        IProductService productService,
        IShieldProvider shieldProvider)
        : base(configuration, tmSocketFactory, tmLogger, plcFactory, plcAddProvider, productService, shieldProvider)
    {
    }

    public override string FlowName => $"{ FlowType.UNSCREW}1";
}

internal class UnscrewFlow2 : ScrewFlowBase
{
    public UnscrewFlow2(
        IConfiguration configuration,
        ITmSocketFactory tmSocketFactory,
        ITmLogger tmLogger,
        IPlcFactory plcFactory,
        IPlcAddProvider plcAddProvider,
        IProductService productService,
        IShieldProvider shieldProvider)
        : base(configuration, tmSocketFactory, tmLogger, plcFactory, plcAddProvider, productService, shieldProvider)
    {
    }

    public override string FlowName => $"{ FlowType.UNSCREW}2";
}
