using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;
internal abstract class FlowBase : IFlowBase
{
    private const int CodeLen = Consts.PRODUCTCODELEN;

    public abstract string FlowName { get; }
    protected abstract ScanAddress ScanDb { get; }
    protected IPlcService Plc => _plcFactory.GetPlc();

    private ITmSocketClient _scanClient;
    private readonly IPlcFactory _plcFactory;
    private readonly IConfiguration _configuration;
    private readonly ITmSocketFactory _tmSocketFactory;
    protected readonly ITmLogger _logger;
    protected readonly IShieldProvider _shiledProvider;
    protected FlowBase(IConfiguration configuration,
                                        ITmSocketFactory tmSocketFactory,
                                        ITmLogger tmLogger,
                                        IPlcFactory plcFactory,
                                        IShieldProvider shieldProvider)
    {
        _configuration = configuration;
        _tmSocketFactory = tmSocketFactory;
        _logger = tmLogger;
        _plcFactory = plcFactory;
        _shiledProvider = shieldProvider;
    }
    protected virtual Task<bool> AfterScanSuccessAsync(string barCode) { return Task.FromResult(true); }
    public virtual Task RunAsync(CancellationToken cancellationToken)
    {
        BuildScanner();
        return TaskHelper.CreateLongRunningTask(HandlerScanFlowAsync, "Scan", 200, _logger, cancellationToken);
    }
    private async Task HandlerScanFlowAsync()
    {
        var trigger = await Plc.ReadInt16Async(ScanDb.ScanTrigger);
        if (trigger.Data != (short)PlcResult.OK) return;
        _logger.LogComm($"[PLC-{ScanDb.ScanTrigger}]扫码触发: {trigger.Data}");
        await ClearPlcAsync(ScanDb.ScanTrigger);
        _logger.LogComm($"[PLC-{ScanDb.ScanTrigger}]扫码触发清零");

        //发送指令
        await _scanClient?.SendAsync("+", System.Text.Encoding.ASCII);
    }
    private void BuildScanner()
    {
        var config = _configuration.GetReworkOption();
        if (string.IsNullOrEmpty(config.Scanner.Ip))
            return;
        var ipPortConfig = new IpPortConfig()
        {
            Ip = config.Scanner.Ip,
            Port = config.Scanner.Port,
            Name = $"Scanner #1",
        };
        var scanClient = _tmSocketFactory.GetOrCreateClient(ipPortConfig);
        scanClient.DataReceived += async p => await ProcessScanResultAsync(p);
        _scanClient = scanClient;
    }

    private async Task ProcessScanResultAsync(string scanResult)
    {
        scanResult = scanResult.Replace("\r", "");
        _logger.LogWorkplaceInfo(1, $"扫码枪扫码结果: {scanResult}");

        var scanNg = scanResult == "Err" || scanResult == "ERR" || scanResult == "ERROR" || scanResult == "NG";

        var shield = await _shiledProvider.GetShieldAsync();
        if (!shield.Sfc && scanNg)
        {
            scanResult = $"Fake-{DateTime.Now.ToString1()}";
            scanNg = scanResult == "Err" || scanResult == "ERR" || scanResult == "ERROR" || scanResult == "NG";
        }

        if (scanNg)
        {
            await ResponseToPlcAsync(ScanDb.ScanResult, PlcResult.NG);
            var ngDesc = $"扫码失败: 2(未识别到有效条码)";
            _logger.LogWorkplaceInfo(1, ngDesc);
            return;
        }

        _logger.LogWorkplaceInfo(1, $"扫码成功，条码:{scanResult}");

        //扫码成功处理 
        var isSuccess = await AfterScanSuccessAsync(scanResult);
        if (isSuccess)
        {
            await WriteCodeAsync(ScanDb.ProductAdd, scanResult);
            await ResponseToPlcAsync(ScanDb.ScanResult, PlcResult.OK);
        }
        else
        {
            await ClearPlcCodeAsync(ScanDb.ProductAdd, CodeLen);
            await ResponseToPlcAsync(ScanDb.ScanResult, PlcResult.NG);
        }
    }

    protected Task ClearPlcAsync(string db) => ResponseToPlcAsync(db, PlcResult.Zero);
    protected async Task ResponseToPlcAsync(string db, PlcResult value)
    {
        var result = await Plc.WriteAsync(db, (short)value);
        if (!result.IsSuccess)
        {
            _logger.LogError($"[{db}] Failed write : {value}  {result.Message}", LogCategory.Comm, LogDisplayMode.TOUI);
        }
    }
    protected async Task ClearPlcCodeAsync(string db, int length)
    {
        var value = new string('\0', length);
        await WriteCodeAsync(db, value);
    }
    protected async Task WriteCodeAsync(string db, string value)
    {
        var result = await Plc.WriteAsync(db, value);
        if (!result.IsSuccess)
        {
            _logger.LogComm($"[PLC-{db}]  Failed write {value}  , {result.Message}");
        }
        else
        {
            _logger.LogComm($"[PLC-{db}] Sucess write {value} ");
        }
    }
}
