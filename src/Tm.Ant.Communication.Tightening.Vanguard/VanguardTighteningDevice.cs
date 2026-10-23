using HslCommunication.ModBus;

using Microsoft.Extensions.Logging;

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
    private ModbusTcpNet _modbus;
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
    public bool IsConnected => !_disposed && _connected;

    public void Configure(object config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (config is not VanguardTighteningDeviceConfig value)
        {
            throw new ArgumentException("VLib设备配置类型错误。", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(value.Ip) || value.Port is < 1 or > 65535
            || value.TimeoutMs < 100)
        {
            throw new ArgumentException("VLib配置要求有效IP、端口及不少于100ms的超时。", nameof(config));
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

            await StopReceiveAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            _modbus = new ModbusTcpNet(_config.Ip, _config.Port, 1)
            {
                AddressStartWithZero = true,
                ConnectTimeOut = _config.TimeoutMs,
                ReceiveTimeOut = _config.TimeoutMs
            };
            var connection = await _modbus.ConnectServerAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (!connection.IsSuccess)
            {
                throw new IOException($"VLib设备({_config.DeviceId})连接失败：{connection.ErrorCode}，{connection.Message}");
            }

            _connected = true;
            _receiveCancellation = new CancellationTokenSource();
            var receiveToken = _receiveCancellation.Token;
            _receiveTask = Task.Run(() => ReceiveAsync(receiveToken), CancellationToken.None);
            return true;
        }
        catch
        {
            _connected = false;
            await StopReceiveAsync().ConfigureAwait(false);
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
            await StopReceiveAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private async Task StopReceiveAsync()
    {
        _connected = false;
        _receiveCancellation?.Cancel();
        // Hsl读取不可取消，先等待超时退出，避免关闭与在途读取竞争导致内部重连。
        await _receiveTask.ConfigureAwait(false);
        _modbus?.ConnectClose();
        _modbus?.Dispose();
        _modbus = null;
        _receiveTask = Task.CompletedTask;
        _receiveCancellation?.Dispose();
        _receiveCancellation = null;
    }

    private ushort[] ReadRegisters(string address, ushort count, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var result = _modbus.ReadUInt16(address, count);
        if (!result.IsSuccess)
        {
            throw new IOException($"VLib设备({_config.DeviceId})读取({address})、字数({count})失败：{result.ErrorCode}，{result.Message}");
        }

        if (result.Content == null || result.Content.Length != count)
        {
            throw new InvalidDataException($"VLib设备({_config.DeviceId})读取({address})数据不足：预期{count}字，实际{result.Content?.Length ?? 0}字。");
        }

        return result.Content;
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
                var currentBusy = (ReadRegisters("194", 1, token)[0] & 2) != 0;
                if (currentBusy && !busy)
                {
                    samplesRead = 0;
                    torques.Clear();
                }

                if (currentBusy || busy)
                {
                    var length = ReadRegisters("x=4;63", 1, token)[0];
                    if (length < samplesRead || length > ushort.MaxValue - 0x40)
                    {
                        throw new InvalidDataException($"VLib曲线长度异常：{length}，已读取：{samplesRead}。");
                    }

                    while (length - samplesRead >= 120 || (!currentBusy && length > samplesRead))
                    {
                        token.ThrowIfCancellationRequested();
                        var count = (ushort)Math.Min(120, length - samplesRead);
                        var values = ReadRegisters($"x=4;{(0x40 + samplesRead).ToString(System.Globalization.CultureInfo.InvariantCulture)}", count, token);
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
            if (ReadRegisters("x=4;48", 1, token)[0] == 1)
            {
                var values = ReadRegisters("x=4;49", 6, token);
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
