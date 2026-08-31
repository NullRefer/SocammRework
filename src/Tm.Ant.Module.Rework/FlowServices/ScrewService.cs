using System.Collections.Concurrent;

using Tm.Ant.Framework.Models.Events;
using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.Models;

using VanguardLibrary;

namespace Tm.Ant.Module.Rework.FlowServices;
public interface IScrewResultMessage
{
    /// <summary>
    /// 接收螺丝批拧紧结果
    /// </summary>
    Action<ScrewResult> ReceiveScrewResult { get; set; }
    /// <summary>
    /// 接收螺丝批拧紧曲线数据
    /// </summary>
    Action<ScrewCurveModel> ReceiveScrewCurve { get; set; }
}
public interface IScrewService : IScrewResultMessage
{
    Task<bool> ConnectAsync();
    Task DisConnectAsync();
    Task<bool> IsConnectedAsync();
}
internal class ScrewService : IScrewService
{
    public Action<ScrewResult> ReceiveScrewResult { get; set; }
    public Action<ScrewCurveModel> ReceiveScrewCurve { get; set; }

    private bool _connected;
    private readonly VLib _vLib;
    private readonly ScrewServiceConfig _config;
    private readonly ITmLogger _logger;
    public ScrewService(ScrewServiceConfig config, ITmLogger logger)
    {
        _config = config;
        _logger = logger;

        _vLib = new VLib();
        _vLib.ResultChanged = OnResultChanged;
        _vLib.ChatDatasChanged = OnChatDatasChanged;
        _vLib.ConnectChanged = c =>
        {
            var flag = c ? "Connected" : "Disconnet";
            _logger.LogComm($"Screw {_config.IpAndPort.Ip} {flag}");
        };
        _vLib.BusyChanged = b =>
        {
            var flag = b ? "Begin work" : "End work";
            _logger.LogComm($"Screw {_config.IpAndPort.Ip} {flag}");
        };
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _connected = _vLib.IsConnected;
            if (!_connected)
            {
                _connected = await _vLib.ConnectAsync(_config.IpAndPort.Ip, _config.IpAndPort.Port);
            }
            return _connected;
        }
        catch (Exception ex)
        {
            _logger.LogComm($"ScrewService ConnectAsync Exception:{ex}");
        }
        return _connected;
    }

    public Task DisConnectAsync()
    {
        return _vLib.DisconnectAsync();
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(_vLib.IsConnected);
    }

    private void OnResultChanged(VResult vResult)
    {
        ReceiveScrewResult?.Invoke(new ScrewResult()
        {
            UseProgramNo = vResult.ProgNo,
            LockTime = vResult.Time,
            Noturns = vResult.Turns,
            Torque = vResult.Torque,
            Result = (int)vResult.Result,
            EndTime = DateTime.Now,
            ScrewId = _config.ScrewId,
        });
    }
    private void OnChatDatasChanged(List<ChatPoint> points)
    {
        var curve = new ScrewCurveModel() { AngleList = new List<float>(), TorqueList = new List<float>(), ScrewId = _config.ScrewId };
        foreach (var p in points)
        {
            curve.AngleList.Add((float)p.Turns);
            curve.TorqueList.Add((float)p.Torque);
        }
        ReceiveScrewCurve?.Invoke(curve);
    }
}
public interface IScrewServiceFactory : ISingleton
{
    IScrewResultMessage GetOrCreate(ScrewServiceConfig config);
}
internal class ScrewServiceFactory : IScrewServiceFactory
{
    private readonly IStatusMonitor _statusMonitor;
    private readonly ITmLogger _tmLogger;

    private readonly ConcurrentDictionary<string, IScrewService> _services = new();
    private static bool _healthCheckRunning = false;

    public ScrewServiceFactory(IStatusMonitor statusMonitor, ITmLogger tmLogger)
    {
        _statusMonitor = statusMonitor;
        _tmLogger = tmLogger;
    }
    public IScrewResultMessage GetOrCreate(ScrewServiceConfig config)
    {
        var key = $"SCREW#{config.ScrewId}";
        if (_services.TryGetValue(key, out var service))
        {
            return service;
        }

        service = new ScrewService(config, _tmLogger);
        _services.TryAdd(key, service);
        _ = service.ConnectAsync();

        if (!_healthCheckRunning)
        {
            _healthCheckRunning = true;
            TaskHelper.CreateLongRunningTask(CheckConnectionsAsync, nameof(CheckConnectionsAsync), 3000, _tmLogger, default);
        }
        return service;
    }

    private async Task CheckConnectionsAsync()
    {
        foreach (var (key, service) in _services)
        {
            var result = await service.IsConnectedAsync();
            _statusMonitor.AddOrUpdate(new StatusMonitorModel
            {
                Name = key,
                SortNo = 9,
                Online = result
            });
        }
    }
}

public abstract class ScrewParam
{
    /// <summary>
    /// 1- 一枪；2- 二枪
    /// </summary>
    public int ScrewId { get; set; }
    //第几颗螺丝
    public int ScrewNum { get; set; } = 1;
    /// <summary>
    /// 锁附模式 0- 预锁，1- 死锁
    /// </summary>
    public int LockMode { get; set; }
    public int UseProgramNo { get; set; }
}
public class ScrewResult : ScrewParam
{
    /// <summary>
    /// 圈数
    /// </summary>
    public double Noturns { get; set; }
    public double Torque { get; set; }
    public double LockTime { get; set; }
    /// <summary>
    /// 最终结果1-OK 2-NG
    /// </summary>
    public int Result { get; set; }
    /// <summary>
    /// 锁附完成时间点
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.Now;
    public override string ToString()
    {
        return $"ScrewId:{ScrewId},ScrewNum:{ScrewNum},Torque:{Torque},LockTime:{LockTime},Result:{Result}";
    }
}
public class ScrewCurveModel : ScrewParam
{
    /// <summary>
    /// 角度 - X
    /// </summary>
    public List<float> AngleList { get; set; } = new();
    /// <summary>
    /// 扭力（mNm) - Y
    /// </summary>
    public List<float> TorqueList { get; set; } = new();

    public void Clear()
    {
        AngleList.Clear();
        TorqueList.Clear();
    }
    public ScrewCurveModel Copy()
    {
        var model = new ScrewCurveModel() { ScrewId = ScrewId };
        foreach (var angle in AngleList)
        {
            model.AngleList.Add(angle);
        }
        foreach (var torque in TorqueList)
        {
            model.TorqueList.Add(torque);
        }
        return model;
    }
}
public class ScrewCurveEvent : PubSubEvent<ScrewCurveEvent>
{
    public string? ProductCode { get; set; }
    public int? Cave { get; set; }
    public ScrewCurveModel ScrewCurve { get; set; }
}
public class ScrewServiceConfig
{
    public IpAndPort IpAndPort { get; set; }
    public int ScrewId { get; set; }
}
