using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class FlowIoService : IFlowIoService
{
    private readonly IDeviceFactory _devices;
    private readonly IConfigService _configService;
    private readonly ILogger<FlowIoService> _logger;

    public FlowIoService(IDeviceFactory devices, IConfigService configService, ILogger<FlowIoService> logger)
    {
        _devices = devices;
        _configService = configService;
        _logger = logger;
    }

    public async Task<OperationResult<string>> ScanAsync(string scannerId, CancellationToken token)
    {
        var response = await _devices.GetDeviceService<IScannerService>(scannerId).TriggerAsync(token);
        if (!response.IsSuccess)
        {
            var switches = await _configService.GetAsync<ShieldModel>(token);
            if (switches.Sfc)
            {
                return OperationResult.Error<string>($"扫码枪({scannerId})触发失败：{response.Message}", response.Code);
            }

            var fakeCode = $"Fake-{DateTime.Now:yyyyMMddHHmmssfff}";
            _logger.LogInformationToUI("扫码NG且SFC已屏蔽，设备({DeviceId})，使用模拟产品码({Code})",
                LogCategory.Comm, scannerId, fakeCode);
            return OperationResult.Success<string>(fakeCode);
        }

        var code = response.Data.TrimControlCharacters();
        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult.Error<string>($"扫码枪({scannerId})条码校验失败：产品码为空。");
        }

        _logger.LogInformationToUI("扫码成功，设备({DeviceId})，产品码({Code})", LogCategory.Comm, scannerId, code);
        return OperationResult.Success<string>(code);
    }

    public Task<OperationResult<short>> ReadShortAsync(string address, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return _devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc).ReadInt16Async(address);
    }

    public async Task<OperationResult<string>> ReadCodeAsync(string address, ushort length, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var result = await _devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc)
            .ReadStringAsync(address, length);
        return result.IsSuccess ? OperationResult.Success<string>(result.Data.TrimControlCharacters()) : result;
    }

    public async Task<OperationResult> WriteAsync(string address, object value, CancellationToken token)
    {
        var plc = _devices.GetDeviceService<IPlcService>(DeviceIds.MainPlc);
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
                _logger.LogInformationToUI("PLC写入完成，地址({Address})，值({Value})，尝试({Attempt})",
                    LogCategory.Comm, DeviceIds.MainPlc, address, JsonConvert.SerializeObject(value), attempt);
                return result;
            }
        }

        _logger.LogErrorToUI("PLC写入失败，地址({Address})，值({Value})，原因({Reason})",
            LogCategory.Comm, DeviceIds.MainPlc, address, JsonConvert.SerializeObject(value), result.Message);
        return result;
    }
}
