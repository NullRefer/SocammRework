using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;
using Tm.Ant.Module.Rework.Models.SFC;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 下料扫码、逐穴判定与过站下线，清零由框架Runtime在结果反馈之后执行。
/// </summary>
internal sealed class UldFlow : IFlowBase
{
    private readonly IFlowRuntimeService _runtime;
    private readonly IFlowIoService _io;
    private readonly IProductService _products;
    private readonly INvdSfcService _sfc;
    private readonly IEventBus _events;
    private readonly IFlowResultState _state;
    private readonly ILogger<UldFlow> _logger;
    private readonly UnloadAddress _address = new();

    public UldFlow(IFlowRuntimeService runtime, IFlowIoService io, IProductService products, INvdSfcService sfc,
        IEventBus events, IFlowResultState state, ILogger<UldFlow> logger)
    {
        _runtime = runtime;
        _io = io;
        _products = products;
        _sfc = sfc;
        _events = events;
        _state = state;
        _logger = logger;
    }

    public string FlowName => FlowType.ULD.ToString();

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
        try
        {
            var scan = await _io.ScanAsync(scannerId, token);
            if (!scan.IsSuccess)
            {
                await _io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料扫码失败：{scan.Message}", scan.Code);
            }

            var pendingDisplay = new FlowEventPubSub { ProductCode = scan.Data, IsOk = false };
            _state.Set(pendingDisplay);
            _events.Publish(pendingDisplay);
            var entity = await _products.GetFlowAsync(scan.Data, token);
            if (entity == null)
            {
                await _io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料查询失败，产品({scan.Data})：未找到在线生产记录。");
            }

            var results = entity.GetFinalResult();
            var isOk = results.Count > 0 && results.Values.All(value => value == "OK");
            var display = new FlowEventPubSub { ProductCode = scan.Data, CaveResults = results, IsOk = isOk };
            _state.Set(display);
            _events.Publish(display);
            var sfcDescription = "质量NG，未上传PASS";
            if (isOk)
            {
                var pass = await _sfc.StationRecordAsync(new StationRecordRequest
                {
                    Sn = scan.Data,
                    Result = "PASS",
                    ErrorCode = "N/A"
                }, token);
                if (!pass.IsSuccess)
                {
                    await _io.WriteAsync(address.Feedback, (short)2, token);
                    return OperationResult.Error($"下料SFC过站失败，产品({scan.Data})：{pass.Result}", pass.Rc);
                }

                sfcDescription = "PASS接口处理成功（屏蔽时模拟成功）";
            }

            var offline = await _products.OfflineFlowAsync(entity, token);
            if (!offline.IsSuccess)
            {
                await _io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料软删除失败：{offline.Message}", offline.Code);
            }

            await _io.WriteAsync(address.ProductCode, scan.Data, token);
            // D201仍为处理成功反馈，产品质量单独展示，不擅自改变已确认的协议含义。
            await _io.WriteAsync(address.Feedback, (short)1, token);
            _logger.LogInformationToUI("下料完成，产品({Code})，需拆({Required})，逐穴结果({Results})，总质量({Quality})，SFC({Sfc})，主表已软下线",
                LogCategory.Comm, scan.Data, entity.UnScrewCaves, JsonConvert.SerializeObject(results),
                isOk ? "OK" : "NG", sfcDescription);
            return OperationResult.Success();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"下料交互异常：{ex.Message}");
        }
    }
}
