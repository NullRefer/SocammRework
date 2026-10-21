using Tm.Ant.Communication.Tightening.Abstractions.Interfaces;
using Tm.Ant.Communication.Tightening.Abstractions.Models;
using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 两台同协议拆螺丝机共用扫码下发、曲线关联与最终结果保存，不管理设备连接。
/// </summary>
internal abstract class ScrewFlowBase : IFlowBase
{
    private readonly IFlowRuntimeService _runtime;
    private readonly IFlowIoService _io;
    private readonly IProductService _products;
    private readonly IScrewCurveProvider _curves;
    private readonly IDeviceFactory _devices;
    private readonly ILogger _logger;
    private readonly ScrewAddress _address = new();

    protected ScrewFlowBase(IFlowRuntimeService runtime, IFlowIoService io, IProductService products,
        IScrewCurveProvider curves, IDeviceFactory devices, ILogger logger)
    {
        _runtime = runtime;
        _io = io;
        _products = products;
        _curves = curves;
        _devices = devices;
        _logger = logger;
    }

    public abstract string FlowName { get; }

    public async Task RunAsync(CancellationToken token)
    {
        if (!_runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner,
            DeviceIds.Tightening1, DeviceIds.Tightening2))
        {
            return;
        }

        var first = _devices.GetDeviceService<ITighteningDevice>(DeviceIds.Tightening1);
        var second = _devices.GetDeviceService<ITighteningDevice>(DeviceIds.Tightening2);
        Func<TighteningCurve, Task> firstCurve = curve => SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 1, curve, token);
        Func<TighteningCurve, Task> secondCurve = curve => SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 2, curve, token);
        first.TighteningCurveReceivedAsync += firstCurve;
        second.TighteningCurveReceivedAsync += secondCurve;
        try
        {
            await Task.WhenAll(
                _runtime.CreatePollingTask(t => _runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
                    "拆螺丝扫码下发", _address.Scan.Trigger,
                    ct => ScanAsync(_address.Scan, DeviceIds.ProductScanner, ct), t), "拆螺丝扫码", token),
                _runtime.CreatePollingTask(async t =>
                {
                    // Runtime的RunOnTrigger在失败后也清零；本交互仅允许保存成功才清零。
                    var result = await SaveResultAsync(_address.Result, t);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarningToUI("穴位结果保存失败：{Reason}", LogCategory.ALARM, result.Message);
                    }
                }, "穴位最终结果保存", token));
        }
        finally
        {
            first.TighteningCurveReceivedAsync -= firstCurve;
            second.TighteningCurveReceivedAsync -= secondCurve;
        }
    }

    private async Task<OperationResult> ScanAsync(ScrewAddress.ScanGroup address, string scannerId,
        CancellationToken token)
    {
        try
        {
            var scan = await _io.ScanAsync(scannerId, token);
            if (!scan.IsSuccess)
            {
                await _io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"拆螺丝扫码失败：{scan.Message}", scan.Code);
            }

            var entity = await _products.GetFlowAsync(scan.Data, token);
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
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"拆螺丝扫码交互异常：{ex.Message}");
        }
    }

    private async Task<OperationResult> SaveResultAsync(ScrewAddress.ResultGroup address, CancellationToken token)
    {
        var result = (await _io.ReadShortAsync(address.Trigger, token)).Data;
        if (result == 0)
        {
            return OperationResult.Success();
        }

        _logger.LogInformationToUI("穴位结果触发，地址({Address})，值({Value})", LogCategory.Comm, address.Trigger, result);
        var code = (await _io.ReadCodeAsync(address.ProductCode, token)).Data?.Trim('\0', ' ');
        var cave = (await _io.ReadShortAsync(address.Cave, token)).Data;
        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult.Error($"保存穴位结果失败，地址({address.ProductCode})：产品码为空，保留结果触发。");
        }

        var saved = await _products.UpdateFlowAsync(code, cave, result, token);
        if (!saved.IsSuccess)
        {
            return OperationResult.Error($"保存穴位结果失败：{saved.Message}，保留结果触发。", saved.Code);
        }

        await _io.WriteAsync(address.Trigger, (short)0, token);
        _logger.LogInformationToUI("穴位拆除结果已保存，产品({Code})，穴位({Cave})，质量({Quality})，已执行清零响应",
            LogCategory.Comm, code, cave, result == 1 ? "OK" : "NG");
        return OperationResult.Success();
    }

    private async Task SaveCurveAsync(ScrewAddress.CurveGroup[] groups, int toolNo, TighteningCurve curve,
        CancellationToken token)
    {
        if (curve.ToolNo != toolNo)
        {
            throw new InvalidDataException($"螺丝批设备({curve.DeviceId})配置编号({curve.ToolNo})与Flow预期({toolNo})不一致。");
        }

        if (curve.Angles.Count != curve.Torques.Count)
        {
            throw new InvalidDataException($"螺丝批({toolNo})曲线角度和扭矩点数不一致。");
        }

        foreach (var group in groups)
        {
            foreach (var screw in group.Screws)
            {
                var plcTool = (await _io.ReadShortAsync(screw.ToolNo, token)).Data;
                if (plcTool != toolNo)
                {
                    continue;
                }

                var code = (await _io.ReadCodeAsync(group.ProductCode, token)).Data?.Trim('\0', ' ');
                var cave = (await _io.ReadShortAsync(group.Cave, token)).Data;
                var program = (await _io.ReadShortAsync(screw.Program, token)).Data;
                if (string.IsNullOrWhiteSpace(code) || cave is < 1 or > 8)
                {
                    continue;
                }

                var item = new ScrewCurveData
                {
                    ProductCode = code,
                    Cave = cave,
                    ScrewCurve = new ScrewCurveModel
                    {
                        ScrewId = toolNo,
                        ScrewNum = screw.Number,
                        ProgramNo = program,
                        AngleList = curve.Angles.ToList(),
                        TorqueList = curve.Torques.ToList()
                    }
                };
                await _curves.SetCurveAsync(item, token);
                _logger.LogInformationToUI("本地曲线保存完成，产品({Code})，穴位({Cave})，螺丝({Screw})，螺丝批({Tool})，点数({Count})",
                    LogCategory.Comm, code, cave, screw.Number, toolNo, curve.Torques.Count);
                return;
            }
        }

        var unassociated = new ScrewCurveData
        {
            ProductCode = "UNMATCHED",
            Cave = 0,
            ScrewCurve = new ScrewCurveModel
            {
                ScrewId = toolNo,
                ScrewNum = 0,
                AngleList = curve.Angles.ToList(),
                TorqueList = curve.Torques.ToList()
            }
        };
        await _curves.SetCurveAsync(unassociated, token);
        _logger.LogWarningToUI("螺丝批({Tool})曲线未匹配有效产品/穴位，原始曲线已保存为UNMATCHED文件，不写业务数据库",
            LogCategory.Comm, toolNo);
    }
}
