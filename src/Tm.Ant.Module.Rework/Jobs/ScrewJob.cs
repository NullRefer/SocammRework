using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Jobs;
internal class ScrewJob : ITmJob
{
    private UnscrewAddress _unscrewAddress;

    private IPlcService Plc => _plcFactory.GetPlc();

    private readonly IPlcFactory _plcFactory;
    private readonly ITmLogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IPlcAddProvider _plcAddProvider;
    private readonly IScrewServiceFactory _screwServiceFactory;
    private readonly IEventBus _eventBus;
    private readonly IScrewCurveProvider _screwCurveProvider;
    private readonly IProductService _productService;
    public ScrewJob(IPlcFactory plcFactory,
        ITmLogger logger,
        IConfiguration configuration,
        IPlcAddProvider plcAddProvider,
        IScrewServiceFactory screwServiceFactory,
        IEventBus eventBus,
        IScrewCurveProvider screwCurveProvider,
        IProductService productService)
    {
        _plcFactory = plcFactory;
        _logger = logger;
        _configuration = configuration;
        _plcAddProvider = plcAddProvider;
        _screwServiceFactory = screwServiceFactory;
        _eventBus = eventBus;
        _screwCurveProvider = screwCurveProvider;
        _productService = productService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var flowName = _configuration.GetCurrentMachine()?.FlowName;
        if (flowName == null) return;
        else if (!flowName.Contains(FlowType.UNSCREW.ToString())) return;

        var config = _configuration.GetReworkOption();
        _unscrewAddress = _plcAddProvider.GetAddress(FlowType.UNSCREW) as UnscrewAddress;

        if (!string.IsNullOrEmpty(config.FirstScrew.Ip))
        {
            var firstScrew = _screwServiceFactory.GetOrCreate(new ScrewServiceConfig() { IpAndPort = config.FirstScrew, ScrewId = 1 });
            firstScrew.ReceiveScrewResult += async p => await OnReceiveScrewResultAsync(p);
            firstScrew.ReceiveScrewCurve += async p => await OnReceiveScrewCurveAsync(p);
        }
        if (!string.IsNullOrEmpty(config.SecondScrew.Ip))
        {
            var secondScrw = _screwServiceFactory.GetOrCreate(new ScrewServiceConfig() { IpAndPort = config.SecondScrew, ScrewId = 2 });
            secondScrw.ReceiveScrewResult += async p => await OnReceiveScrewResultAsync(p);
            secondScrw.ReceiveScrewCurve += async p => await OnReceiveScrewCurveAsync(p);
        }
        await TaskHelper.CreateLongRunningTask(() => RecordUnscrewCurveAsync(), $"RecordUnscrewCurveAsync", 200, _logger, cancellationToken);
    }

    private async Task RecordUnscrewCurveAsync()
    {
        var doResult = await Plc.ReadInt16Async(_unscrewAddress.Workplace2.UnscrewResult);
        if (doResult.IsSuccess && doResult.Data > 0)
        {
            var product2 = await Plc.ReadStringAsync(_unscrewAddress.Workplace2.ProductDb, Consts.PRODUCTCODELEN);
            var prodcutCode = product2.Data;
            var cave = (await Plc.ReadInt16Async(_unscrewAddress.Workplace2.DoCaveDb)).Data;
            var result = doResult.Data;
            await ClearPlcAsync(_unscrewAddress.Workplace2.UnscrewResult);
            _logger.LogComm($"(PLC-{_unscrewAddress.Workplace2.UnscrewResult}) 产品{prodcutCode}，穴位({cave})拆完螺丝，结果:{result}");
            await _productService.UpdateFlowAsync(prodcutCode, cave, result);
        }
    }

    private async Task<ScrewInfo> GetInfoByScrewIdAsync(int screwId)
    {
        var gunNo = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.FirstScrewNo)).Data;
        if (gunNo == screwId)
        {
            await ClearPlcAsync(_unscrewAddress.Workplace1.FirstScrewNo);
            var product1 = await Plc.ReadStringAsync(_unscrewAddress.Workplace1.ProductDb, Consts.PRODUCTCODELEN);
            return new ScrewInfo()
            {
                ScrewNum = 1,
                Cave = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.DoCaveDb)).Data,
                GunNo = screwId,
                PGNum = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.FirstScrewPGNum)).Data,
                ProductCode = product1.Data,
            };
        }
        gunNo = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.ThirdScrewNo)).Data;
        if (gunNo == screwId)
        {
            await ClearPlcAsync(_unscrewAddress.Workplace1.ThirdScrewNo);
            var product1 = await Plc.ReadStringAsync(_unscrewAddress.Workplace1.ProductDb, Consts.PRODUCTCODELEN);
            return new ScrewInfo()
            {
                ScrewNum = 3,
                Cave = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.DoCaveDb)).Data,
                GunNo = screwId,
                PGNum = (await Plc.ReadInt16Async(_unscrewAddress.Workplace1.ThirdScrewPGNum)).Data,
                ProductCode = product1.Data,
            };
        }
        gunNo = (await Plc.ReadInt16Async(_unscrewAddress.Workplace2.SecondScrewNo)).Data;
        if (gunNo == screwId)
        {
            await ClearPlcAsync(_unscrewAddress.Workplace2.SecondScrewNo);
            var product2 = await Plc.ReadStringAsync(_unscrewAddress.Workplace2.ProductDb, Consts.PRODUCTCODELEN);
            return new ScrewInfo()
            {
                ScrewNum = 2,
                Cave = (await Plc.ReadInt16Async(_unscrewAddress.Workplace2.DoCaveDb)).Data,
                GunNo = screwId,
                PGNum = (await Plc.ReadInt16Async(_unscrewAddress.Workplace2.SecondScrewPGNum)).Data,
                ProductCode = product2.Data,
            };
        }
        return null;
    }

    private async Task OnReceiveScrewResultAsync(ScrewResult screwResult)
    {
        await Task.CompletedTask;
        _logger.LogComm($"Screw result : {JsonHelper.SerializeObject(screwResult)}");
    }
    private async Task OnReceiveScrewCurveAsync(ScrewCurveModel screwCurveModel)
    {
        try
        {
            var screwInfo = await GetInfoByScrewIdAsync(screwCurveModel.ScrewId);
            //如果找不到，则创建一个fake给图像保存数据使用
            if (screwInfo == null)
            {
                screwInfo = ScrewInfo.FakeScrewInfo;
                screwInfo.GunNo = screwCurveModel.ScrewId;
            }

            screwCurveModel.ScrewNum = screwInfo.ScrewNum;
           
            var @event = new ScrewCurveEvent() { ScrewCurve = screwCurveModel, ProductCode = screwInfo.ProductCode, Cave = screwInfo.Cave };
            //保存曲线数据
            await _screwCurveProvider.SetCurveAsync(@event);

            //发布曲线数据出去进行展示
            _eventBus.Publish(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }
    private Task ClearPlcAsync(string db) => ResponseToPlcAsync(db, PlcResult.Zero);
    private async Task ResponseToPlcAsync(string db, PlcResult value)
    {
        await Plc.WriteAsync(db, (short)value);
        _logger.LogComm($"PLC-{db} write value : {value}");
    }
}

public class ScrewInfo
{
    public string ProductCode { get; set; }
    public int Cave { get; set; }
    public int ScrewNum { get; set; }
    public int PGNum { get; set; }

    public int GunNo { get; set; }

    public static ScrewInfo FakeScrewInfo => new ScrewInfo()
    {
        Cave = 1,
        ProductCode = $"Fake{Guid.NewGuid().ToString().Substring(0, 9)}",
        PGNum = 1,
        ScrewNum = 1
    };
}
