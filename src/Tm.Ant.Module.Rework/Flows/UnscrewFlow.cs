using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;
internal abstract class UnscrewFlowBase : FlowBase
{
    private readonly UnscrewAddress _unscrewAddress;
    private readonly IProductService _productService;
    public UnscrewFlowBase(IConfiguration configuration,
                                                ITmSocketFactory tmSocketFactory,
                                                ITmLogger tmLogger,
                                                IPlcFactory plcFactory,
                                                IPlcAddProvider plcAddProvider,
                                                IProductService productService,
                                                IShieldProvider shieldProvider)
        : base(configuration, tmSocketFactory, tmLogger, plcFactory, shieldProvider)
    {
        _productService = productService;
        _unscrewAddress = plcAddProvider.GetAddress(FlowType.UNSCREW) as UnscrewAddress;
    }
    protected override ScanAddress ScanDb => _unscrewAddress;

    protected override async Task<bool> AfterScanSuccessAsync(string barCode)
    {
        try
        {
            var flowMain = await _productService.GetFlowAsync(barCode);

            if (flowMain != null)
            {
                var caves = StringToshorts(flowMain.UnScrewCaves);

                //notify PLC work...
                await Plc.WriteAsync(_unscrewAddress.UnscrewFlag, (short)1);
                _logger.LogComm($"[Write PLC-{_unscrewAddress.UnscrewFlag}] value: 1");

                await Plc.WriteAsync(_unscrewAddress.UnscrewLocationDb, caves);
                _logger.LogComm($"[Write PLC-{_unscrewAddress.UnscrewLocationDb}] {caves}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        return await base.AfterScanSuccessAsync(barCode);
    }

    private short[] StringToshorts(string strCaves)
    {
        var values = new short[8];
        var arr = strCaves.Split(',');
        for (int i = 0; i < arr.Length; i++)
        {
            values[i] = short.Parse(arr[i]);
        }
        return values;
    }
}

internal class UnscrewFlow1 : UnscrewFlowBase
{
    public UnscrewFlow1(IConfiguration configuration, ITmSocketFactory tmSocketFactory, ITmLogger tmLogger, IPlcFactory plcFactory, IPlcAddProvider plcAddProvider, IProductService productService, IShieldProvider shieldProvider) : base(configuration, tmSocketFactory, tmLogger, plcFactory, plcAddProvider, productService, shieldProvider)
    {
    }

    public override string FlowName => $"{ FlowType.UNSCREW}1";
}

internal class UnscrewFlow2 : UnscrewFlowBase
{
    public UnscrewFlow2(IConfiguration configuration, ITmSocketFactory tmSocketFactory, ITmLogger tmLogger, IPlcFactory plcFactory, IPlcAddProvider plcAddProvider, IProductService productService, IShieldProvider shieldProvider) : base(configuration, tmSocketFactory, tmLogger, plcFactory, plcAddProvider, productService, shieldProvider)
    {
    }

    public override string FlowName => $"{ FlowType.UNSCREW}2";
}
