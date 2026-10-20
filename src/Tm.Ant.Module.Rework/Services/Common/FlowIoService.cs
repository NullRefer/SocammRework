using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class FlowIoService(IDeviceFactory devices, IShieldProvider shields, ILogger<FlowIoService> logger)
    : IFlowIoService
{
    public async Task<OperationResult<string>> ScanAsync(string scannerId, CancellationToken token)
    {
        var response = await devices.GetDeviceService<IScannerService>(scannerId).TriggerAsync(token);
        if (!response.IsSuccess)
        {
            return OperationResult.Error<string>($"扫码枪({scannerId})触发失败：{response.Message}", response.Code);
        }

        var code = new string((response.Data ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        var switches = await shields.GetShieldAsync(token);
        if (!switches.Sfc && code is "Err" or "ERR" or "ERROR" or "NG")
        {
            code = $"Fake-{DateTime.Now:yyyyMMddHHmmssfff}";
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length > Consts.PRODUCTCODELEN
            || code is "Err" or "ERR" or "ERROR" or "NG")
        {
            return OperationResult.Error<string>($"扫码枪({scannerId})条码校验失败：'{code}'，要求1~30字符的有效产品码。");
        }

        logger.LogInformationToUI("扫码成功，设备({DeviceId})，产品码({Code})", LogCategory.Comm, scannerId, code);
        return OperationResult.Success<string>(code);
    }

    public Task<OperationResult<short>> ReadShortAsync(string address, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc).ReadInt16Async(address);
    }

    public Task<OperationResult<string>> ReadCodeAsync(string address, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc).ReadStringAsync(address, Consts.PRODUCTCODELEN);
    }

    public async Task<OperationResult> WriteAsync(string address, object value, CancellationToken token)
    {
        var plc = devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc);
        OperationResult result = OperationResult.NotConnected;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            token.ThrowIfCancellationRequested();
            result = value switch
            {
                short number => await plc.WriteAsync(address, number),
                string text => await plc.WriteAsync(address, text),
                short[] array => await plc.WriteAsync(address, array),
                _ => throw new ArgumentException($"PLC写入类型不支持：{value?.GetType().Name}", nameof(value))
            };
            if (result.IsSuccess)
            {
                logger.LogInformationToUI("PLC写入完成，设备({DeviceId})，地址({Address})，值({Value})，尝试({Attempt})",
                    LogCategory.Comm, DeviceIds.MainPlc, address, JsonConvert.SerializeObject(value), attempt);
                return result;
            }
        }

        logger.LogErrorToUI("PLC写入失败，设备({DeviceId})，地址({Address})，值({Value})，原因({Reason})",
            LogCategory.Comm, DeviceIds.MainPlc, address, JsonConvert.SerializeObject(value), result.Message);
        return result;
    }
}
