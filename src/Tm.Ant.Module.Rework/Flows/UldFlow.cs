using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;
internal class UldFlow : FlowBase
{
    private readonly ScanAddress _scanAddress;

    private readonly INvdSfcService _nvdSfcService;
    private readonly IProductService _productService;
    private readonly IEventBus _eventBus;
    public UldFlow(IConfiguration configuration,
        ITmSocketFactory tmSocketFactory,
        ITmLogger tmLogger,
        IPlcFactory plcFactory,
        IPlcAddProvider plcAddProvider,
        INvdSfcService nvdSfcService,
        IProductService productService,
         IShieldProvider shieldProvider,
         IEventBus eventBus)
        : base(configuration, tmSocketFactory, tmLogger, plcFactory, shieldProvider)
    {
        _nvdSfcService = nvdSfcService;
        _productService = productService;
        _eventBus = eventBus;
        _scanAddress = plcAddProvider.GetAddress(FlowType.ULD);
    }

    public override string FlowName => FlowType.ULD.ToString();

    protected override ScanAddress ScanDb => _scanAddress;

    // TODO : upload sfc
    protected override async Task<bool> AfterScanSuccessAsync(string barCode)
    {
        //offline data
        var flowMain = await _productService.OfflineFlowAsync(barCode);

        if (flowMain != null)
        {
            try
            {
                var result = flowMain.GetFinalResult();

                //post SFC ,when finally result without 'NG'
                var hasNg = result.Values.Any(t => t == "NG");
                if(!hasNg)
                {
                    var sfcResult = await _nvdSfcService.StationRecordAsync(new RecoredRequest()
                    {
                        Sn = barCode,
                        ErrorCode = "N/A",
                        Result = "PASS",
                    });
                    if (sfcResult.IsSuccess)
                    {
                        _logger.LogInformation($"{barCode} SFC 过站成功: {sfcResult.ToJson()}");
                    }
                    else
                    {
                        _logger.LogInformation($"{barCode} SFC 过站失败: {sfcResult.ToJson()}");
                    }
                }

                _logger.LogInformation($"{barCode} 未完成预定的拆除, 不过站, 请检查拆除机是否正常进行拆除, 当前拆除数据: {result.ToJson()} ");

                _eventBus.Publish(new FlowEventPubSub()
                {
                    ProductCode = flowMain.ProductCode,
                    CaveResults = result,
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString(), LogCategory.Comm);
            }
        }

        return true;
    }
}
