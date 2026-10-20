using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using Modbus.Device;

using Tm.Ant.Communication.Attributes;
using Tm.Ant.Communication.Infra;
using Tm.Ant.Communication.Tightening.Abstractions.Interfaces;
using Tm.Ant.Communication.Tightening.Abstractions.Models;

namespace Tm.Ant.Communication.Tightening.Vanguard;

/// <summary>
/// 将 VLib 寄存器协议接入统一拧紧设备；连接与重连由框架生命周期管理。
/// </summary>
[DeviceService(typeof(ITighteningDevice), typeof(VanguardTighteningDeviceConfig),
    DeviceCategory.Tightening, SubType, "Vanguard VLib Modbus TCP")]
public sealed class VanguardTighteningDevice : ITighteningDevice, IDisposable
{
    public const string SubType = "VanguardVLib";
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private VanguardTighteningDeviceConfig _config;
    private TcpClient _client;
    private ModbusIpMaster _master;
    private CancellationTokenSource _receiveCancellation;
    private Task _receiveTask = Task.CompletedTask;
    private volatile bool _connected;
    private bool _disposed;

    public VanguardTighteningDevice(ILogger<VanguardTighteningDevice> logger)
    {
        _logger = logger;
    }

    public event Func<TighteningResult, Task> TighteningResultReceivedAsync;
    public event Func<TighteningCurve, Task> TighteningCurveReceivedAsync;
    public bool IsConnected => !_disposed && _connected && _client?.Connected == true;

    public void Configure(object config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (config is not VanguardTighteningDeviceConfig value)
        {
            throw new ArgumentException("VLib设备配置类型错误。", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(value.Ip) || value.Port is < 1 or > 65535
            || value.ToolNo is < 1 or > 2 || value.TimeoutMs < 100)
        {
            throw new ArgumentException("VLib配置要求有效IP、端口、螺丝批编号1/2及不少于100ms的超时。", nameof(config));
        }

        DisconnectAsync().GetAwaiter().GetResult();
        _config = new VanguardTighteningDeviceConfig
        {
            DeviceId = value.DeviceId,
            DisplayName = value.DisplayName,
            Ip = value.Ip,
            Port = value.Port,
            ToolNo = value.ToolNo,
            TimeoutMs = value.TimeoutMs
        };
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        await _lifecycle.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsConnected)
            {
                return true;
            }

            if (_config == null)
            {
                return false;
            }

            _master?.Dispose();
            _client?.Dispose();
            _receiveCancellation?.Dispose();
            _client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(_config.TimeoutMs);
            await _client.ConnectAsync(_config.Ip, _config.Port, timeout.Token).ConfigureAwait(false);
            _client.ReceiveTimeout = _config.TimeoutMs;
            _client.SendTimeout = _config.TimeoutMs;
            _master = ModbusIpMaster.CreateIp(_client);
            _master.Transport.ReadTimeout = _config.TimeoutMs;
            _master.Transport.WriteTimeout = _config.TimeoutMs;
            _master.Transport.Retries = 0;
            _connected = true;
            _receiveCancellation = new CancellationTokenSource();
            var receiveToken = _receiveCancellation.Token;
            _receiveTask = Task.Run(() => ReceiveAsync(receiveToken), CancellationToken.None);
            return true;
        }
        catch
        {
            _connected = false;
            _master?.Dispose();
            _client?.Dispose();
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public Task<bool> CheckConnectionAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(IsConnected);
    }

    public async Task DisconnectAsync(CancellationToken token = default)
    {
        await _lifecycle.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _connected = false;
            _receiveCancellation?.Cancel();
            _client?.Dispose();
            await _receiveTask.ConfigureAwait(false);
            _master?.Dispose();
            _master = null;
            _client = null;
            _receiveCancellation?.Dispose();
            _receiveCancellation = null;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private async Task ReceiveAsync(CancellationToken token)
    {
        var busy = false;
        var samplesRead = 0;
        var torques = new List<float>();
        try
        {
            while (!token.IsCancellationRequested)
            {
                var currentBusy = (_master.ReadHoldingRegisters(1, 0xc2, 1)[0] & 2) != 0;
                if (currentBusy && !busy)
                {
                    samplesRead = 0;
                    torques.Clear();
                }

                if (currentBusy || busy)
                {
                    var length = _master.ReadInputRegisters(1, 0x3f, 1)[0];
                    if (length < samplesRead || length > ushort.MaxValue - 0x40)
                    {
                        throw new InvalidDataException($"VLib曲线长度异常：{length}，已读取：{samplesRead}。");
                    }

                    while (length - samplesRead >= 120 || (!currentBusy && length > samplesRead))
                    {
                        token.ThrowIfCancellationRequested();
                        var count = (ushort)Math.Min(120, length - samplesRead);
                        var values = _master.ReadInputRegisters(1, (ushort)(0x40 + samplesRead), count);
                        torques.AddRange(values.Select(value => value / 10f));
                        samplesRead += count;
                    }
                }

                if (busy && !currentBusy)
                {
                    var result = await ReadResultAsync(token).ConfigureAwait(false);
                    if (result != null)
                    {
                        _logger.LogInformation(
                            "螺丝批结果，设备({DeviceId})，批号({Tool})，程序({Program})，质量({Result})，扭矩({Torque})，圈数({Turns})，耗时({Duration})",
                            result.DeviceId, result.ToolNo, result.ProgramName, result.ResultCode,
                            result.Torque, result.Turns, result.Duration);
                        await PublishAsync(TighteningResultReceivedAsync, result, token).ConfigureAwait(false);
                    }

                    // 原VLib波形横坐标每点加1，保留采样步进，不凭空推算时间采样率。
                    var curve = new TighteningCurve
                    {
                        DeviceId = _config.DeviceId,
                        ToolNo = _config.ToolNo,
                        ResultId = null,
                        Angles = Enumerable.Range(1, torques.Count).Select(i => (float)i).ToArray(),
                        Torques = torques.ToArray(),
                        Times = Array.Empty<float>(),
                        ReceivedAt = DateTime.Now
                    };
                    await PublishAsync(TighteningCurveReceivedAsync, curve, token).ConfigureAwait(false);
                }

                busy = currentBusy;
                await Task.Delay(10, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            _logger.LogError(ex, "VLib设备({DeviceId})采集失败，等待生命周期管理器重连", _config.DeviceId);
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            _connected = false;
        }
    }

    private async Task<TighteningResult> ReadResultAsync(CancellationToken token)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            token.ThrowIfCancellationRequested();
            if (_master.ReadInputRegisters(1, 0x30, 1)[0] == 1)
            {
                var values = _master.ReadInputRegisters(1, 0x31, 6);
                return new TighteningResult
                {
                    DeviceId = _config.DeviceId,
                    ToolNo = _config.ToolNo,
                    ResultId = 0,
                    ProgramName = values[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Turns = values[1] / 10d,
                    Angle = values[1] / 10d * 360,
                    Torque = values[2] / 10d,
                    FinalTorque = values[2] / 10d,
                    Duration = TimeSpan.FromMilliseconds(values[3]),
                    ResultCode = values[4],
                    IsOk = values[4] == 1,
                    CompletedAt = DateTime.Now
                };
            }

            await Task.Delay(30, token).ConfigureAwait(false);
        }

        _logger.LogWarning("VLib设备({DeviceId})工作结束后未收到有效结果寄存器，仍发布已采集曲线", _config.DeviceId);
        return null;
    }

    private async Task PublishAsync<T>(Func<T, Task> handlers, T data, CancellationToken token)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Func<T, Task> handler in handlers.GetInvocationList())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                await handler(data).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VLib设备({DeviceId})数据订阅者处理失败", _config.DeviceId);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisconnectAsync().GetAwaiter().GetResult();
        _disposed = true;
        TighteningResultReceivedAsync = null;
        TighteningCurveReceivedAsync = null;
        _lifecycle.Dispose();
    }
}
