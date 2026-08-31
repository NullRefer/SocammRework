using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;
internal class LDFlow : FlowBase
{
    private readonly ScanAddress _scanAddress;

    private readonly INvdSfcService _nvdSfcService;
    private readonly IProductService _productService;
    public LDFlow(IConfiguration configuration,
                              ITmSocketFactory tmSocketFactory,
                              ITmLogger tmLogger,
                              IPlcFactory plcFactory,
                              IPlcAddProvider plcAddProvider,
                              INvdSfcService nvdSfcService,
                              IProductService productService,
                              IShieldProvider shieldProvider)
        : base(configuration, tmSocketFactory, tmLogger, plcFactory, shieldProvider)
    {
        _nvdSfcService = nvdSfcService;
        _productService = productService;
        _scanAddress = plcAddProvider.GetAddress(FlowType.LD);
    }
    public override string FlowName => FlowType.LD.ToString();

    protected override ScanAddress ScanDb => _scanAddress;

    protected override async Task<bool> AfterScanSuccessAsync(string barCode)
    {
        //sfc match
        (bool isSuccess, string caves, string locations) = await MatchReworkCaveAsync(barCode);
        return isSuccess ? await _productService.AddFlowAsync(barCode, caves, locations) : false;
    }

    //TODO
    private async Task<(bool isSuccess, string caves, string locations)> MatchReworkCaveAsync(string productCode)
    {
        //query sfc && match rework caves.

        var resp = await _nvdSfcService.GetCavesAsync(productCode);
        if (resp == null)
        {
            _logger.LogComm($"调用重工接口失败:{resp?.Result}({resp?.Rc})");
            return (false, "", "");
        }

        if (!resp.IsSuccess)
        {
            _logger.LogComm($"{productCode} SFC 反馈不需要拆，请联系生产确认，SFC 反馈信息: {resp?.Result}({resp?.Rc})");
            return (false, "", "");
        }
        if (resp.PcbNg)
        {
            _logger.LogComm($"PCB板不匹配当前工站，无需拆螺丝!");
            return (false, "", "");
        }

        var strCaves = string.Join(',', resp.DoCaves.Select(x => x.ToString()).ToList());
        _logger.LogComm($"需要拆螺丝位置为{resp.Locations}，对应机台穴位为{strCaves}");
        return (true, strCaves, resp.Locations);
    }
}
