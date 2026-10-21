using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 两台同协议拆螺丝机共用扫码下发与PLC穴位最终结果保存。
/// </summary>
internal abstract class ScrewFlowBase : IFlowBase
{
    private readonly IFlowRuntimeService _runtime;
    private readonly IFlowIoService _io;
    private readonly IProductService _productService;
    private readonly ILogger _logger;
    private readonly ScrewAddress _address = new();

    protected ScrewFlowBase(IFlowRuntimeService runtime, IFlowIoService io, IProductService productService,
        ILogger logger)
    {
        _runtime = runtime;
        _io = io;
        _productService = productService;
        _logger = logger;
    }

    public abstract string FlowName { get; }

    public Task RunAsync(CancellationToken token)
    {
        if (!_runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner))
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            _runtime.CreatePollingTask(t => _runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
                "拆螺丝扫码下发", _address.Scan.Trigger,
                ct => ScanAsync(_address.Scan, DeviceIds.ProductScanner, ct), t), "拆螺丝扫码", token),
            _runtime.CreatePollingTask(t => _runtime.RunOnTriggerValueAsync(DeviceIds.MainPlc,
                "穴位最终结果保存", _address.Result.Trigger,
                (value, ct) => SaveResultAsync(_address.Result, value, ct), t), "穴位最终结果保存", token));
    }

    private async Task<OperationResult> ScanAsync(ScrewAddress.ScanGroup address, string scannerId,
        CancellationToken token)
    {
        var scan = await _io.ScanAsync(scannerId, token);
        if (!scan.IsSuccess)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"拆螺丝扫码失败：{scan.Message}", scan.Code);
        }

        var entity = await _productService.GetFlowAsync(scan.Data, token);
        if (entity == null)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"拆螺丝查询失败，产品({scan.Data})：无在线上料记录，未下发加工数据。");
        }

        var required = entity.GetRequiredCaves();
        await _io.WriteAsync(address.RequiredCaves, required, token);
        await _io.WriteAsync(address.ProductCode, scan.Data, token);
        await _io.WriteAsync(address.Updated, (short)1, token);
        await _io.WriteAsync(address.Feedback, (short)1, token);
        _logger.LogInformationToUI("拆螺丝扫码完成，产品({Code})，在线记录({Id})，下发需拆({Caves})、更新标记及扫码成功",
            LogCategory.Comm, scan.Data, entity.Id, entity.UnScrewCaves);
        return OperationResult.Success();
    }

    private async Task<OperationResult> SaveResultAsync(ScrewAddress.ResultGroup address, short result,
        CancellationToken token)
    {
        var code = (await _io.ReadCodeAsync(address.ProductCode, token)).Data;
        var cave = (await _io.ReadShortAsync(address.Cave, token)).Data;
        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult.Error($"保存穴位结果失败，地址({address.ProductCode})：产品码为空。");
        }

        var saved = await _productService.UpdateFlowAsync(code, cave, result, token);
        if (!saved.IsSuccess)
        {
            return OperationResult.Error($"保存穴位结果失败：{saved.Message}", saved.Code);
        }

        _logger.LogInformationToUI("穴位拆除结果已保存，产品({Code})，穴位({Cave})，质量({Quality})",
            LogCategory.Comm, code, cave, result == 1 ? "OK" : "NG");
        return OperationResult.Success();
    }
}

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
