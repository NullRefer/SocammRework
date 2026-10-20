using Tm.Ant.Communication.Tightening.Abstractions.Interfaces;
using Tm.Ant.Communication.Tightening.Abstractions.Models;
using Tm.Ant.Module.Rework.Interfaces.Flows;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// 两台同协议拆螺丝机共用的设备订阅与轮询入口，不管理设备连接。
/// </summary>
internal abstract class ScrewFlowBase(
    IFlowRuntimeService runtime, IScrewService service, IDeviceFactory devices, ILogger logger) : IFlowBase
{
    private readonly ScrewAddress _address = new();
    public abstract string FlowName { get; }

    public async Task RunAsync(CancellationToken token)
    {
        if (!runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner,
            DeviceIds.Tightening1, DeviceIds.Tightening2))
        {
            return;
        }

        var first = devices.GetDeviceService<ITighteningDevice>(DeviceIds.Tightening1);
        var second = devices.GetDeviceService<ITighteningDevice>(DeviceIds.Tightening2);
        Func<TighteningCurve, Task> firstCurve = curve => service.SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 1, curve, token);
        Func<TighteningCurve, Task> secondCurve = curve => service.SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 2, curve, token);
        first.TighteningCurveReceivedAsync += firstCurve;
        second.TighteningCurveReceivedAsync += secondCurve;
        try
        {
            await Task.WhenAll(
                runtime.CreatePollingTask(t => runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
                    "拆螺丝扫码下发", _address.Scan.Trigger,
                    ct => service.ScanAsync(_address.Scan, DeviceIds.ProductScanner, ct), t), "拆螺丝扫码", token),
                runtime.CreatePollingTask(async t =>
                {
                    // Runtime的RunOnTrigger在失败后也清零；本交互仅允许保存成功才清零。
                    var result = await service.SaveResultAsync(_address.Result, t);
                    if (!result.IsSuccess)
                    {
                        logger.LogWarningToUI("穴位结果保存失败：{Reason}", LogCategory.ALARM, result.Message);
                    }
                }, "穴位最终结果保存", token));
        }
        finally
        {
            first.TighteningCurveReceivedAsync -= firstCurve;
            second.TighteningCurveReceivedAsync -= secondCurve;
        }
    }
}
