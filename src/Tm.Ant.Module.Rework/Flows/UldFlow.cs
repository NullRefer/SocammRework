using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 下料扫码、逐穴判定与过站下线，清零由框架Runtime在结果反馈之后执行。
/// </summary>
internal sealed class UldFlow : IFlowBase
{
    private readonly IFlowRuntimeService _runtime;
    private readonly IFlowIoService _io;
    private readonly IProductService _productService;
    private readonly INvdSfcService _nvdSfcService;
    private readonly ILogger<UldFlow> _logger;
    private readonly IScrewDisplayService _screwDisplayService;
    private readonly UnloadAddress _address = new();

    public UldFlow(IFlowRuntimeService runtime, IFlowIoService io, IProductService productService,
        INvdSfcService nvdSfcService, ILogger<UldFlow> logger, IScrewDisplayService screwDisplayService)
    {
        _runtime = runtime;
        _io = io;
        _productService = productService;
        _nvdSfcService = nvdSfcService;
        _logger = logger;
        _screwDisplayService = screwDisplayService;
    }

    public string FlowName => "ULD";

    public Task RunAsync(CancellationToken token)
    {
        if (!_runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner))
        {
            return Task.CompletedTask;
        }

        return _runtime.CreatePollingTask(t => _runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
            "下料扫码判定", _address.Trigger,
            ct => ScanAsync(_address, DeviceIds.ProductScanner, ct), t), FlowName, token);
    }

    private async Task<OperationResult> ScanAsync(UnloadAddress address, string scannerId, CancellationToken token)
    {
        var scan = await _io.ScanAsync(scannerId, token);
        if (!scan.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"下料扫码失败：{scan.Message}", scan.Code);
        }

        var entity = await _productService.GetFlowAsync(scan.Data, token);
        if (entity == null)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"下料查询失败，产品({scan.Data})：未找到在线生产记录。");
        }

        var results = entity.GetFinalResult();
        var resultTime = DateTime.Now;
        _screwDisplayService.Publish(scan.Data, resultTime, results);

        var isOk = results.Count > 0 && results.Values.All(value => value);
        var sfcDescription = "质量NG，未上传PASS";
        if (isOk)
        {
            var pass = await _nvdSfcService.StationRecordAsync(scan.Data, token);
            if (!pass.IsSuccess)
            {
                await _io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料SFC过站失败，产品({scan.Data})：{pass.Message}", pass.Code);
            }

            sfcDescription = "PASS接口处理成功（屏蔽时模拟成功）";
        }

        var offline = await _productService.OfflineFlowAsync(entity, token);
        if (!offline.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"下料软删除失败：{offline.Message}", offline.Code);
        }

        await _io.WriteAsync(address.ProductCode, scan.Data, token);
        // D201仅表示下料处理结果，不表示产品总质量。
        await _io.WriteAsync(address.Feedback, (short)1, token);
        _logger.LogInformationToUI("下料完成，产品({Code})，需拆({Required})，逐穴结果({Results})，总质量({Quality})，SFC({Sfc})，主表已软下线",
            LogCategory.Comm, scan.Data, entity.UnScrewCaves, JsonConvert.SerializeObject(results),
            isOk ? "OK" : "NG", sfcDescription);
        return OperationResult.Success();
    }
}
