using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 上料扫码、SFC确认与建档，清零由框架Runtime在结果反馈之后执行。
/// </summary>
internal sealed class LDFlow : IFlowBase
{
    private readonly IFlowRuntimeService _runtime;
    private readonly IFlowIoService _io;
    private readonly INvdSfcService _nvdSfcService;
    private readonly IProductService _productService;
    private readonly ILogger<LDFlow> _logger;
    private readonly LoadAddress _address = new();

    public LDFlow(IFlowRuntimeService runtime, IFlowIoService io, INvdSfcService nvdSfcService,
        IProductService productService, ILogger<LDFlow> logger)
    {
        _runtime = runtime;
        _io = io;
        _nvdSfcService = nvdSfcService;
        _productService = productService;
        _logger = logger;
    }

    public string FlowName => "LD";

    public Task RunAsync(CancellationToken token)
    {
        if (!_runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner))
        {
            return Task.CompletedTask;
        }

        return _runtime.CreatePollingTask(t => _runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
            "上料扫码建档", _address.Trigger,
            ct => ScanAsync(_address, DeviceIds.ProductScanner, ct), t), FlowName, token);
    }

    private async Task<OperationResult> ScanAsync(LoadAddress address, string scannerId, CancellationToken token)
    {
        var scan = await _io.ScanAsync(scannerId, token);
        if (!scan.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"上料扫码失败：{scan.Message}", scan.Code);
        }

        var response = await _nvdSfcService.GetCavesAsync(scan.Data, token);
        if (!response.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"上料SFC查询失败，产品({scan.Data})：{response.Message}", response.Code);
        }

        var saved = await _productService.AddFlowAsync(scan.Data, response.Data.Caves, response.Data.Locations, token);
        if (!saved.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"上料建档失败：{saved.Message}", saved.Code);
        }

        await _io.WriteAsync(address.ProductCode, scan.Data, token);
        await _io.WriteAsync(address.Feedback, (short)1, token);
        _logger.LogInformationToUI("上料完成，产品({Code})，SFC位置({Locations})，需拆({Caves})，建档ID({Id})",
            LogCategory.Comm, scan.Data, response.Data.Locations, saved.Data.UnScrewCaves, saved.Data.Id);
        return OperationResult.Success();
    }
}
