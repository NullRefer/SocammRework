using System.Net.Sockets;
using System.Net;

using Tm.Ant.CommonBLL.Model;
using Tm.Ant.CommonBLL.Services;
using Tm.Ant.DataAccess.Entity;
using Tm.Ant.CommonBLL.Interfaces;
using System.IO;

namespace Tm.Ant.Module.Rework.FrmOverride;

/// <summary>
/// 系统资源监控job
/// </summary>
internal class NSystemMonitorJob : ITmJob
{
    private readonly ISystemMonitorService _systemMonitorService;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly IEventLogService _eventLogService;
    public NSystemMonitorJob(ISystemMonitorService systemMonitorService,
        IRepositoryFactory repositoryFactory,
        IConfiguration configuration,
        ILogger<NSystemMonitorJob> logger,
        IEventLogService eventLogService)
    {
        _systemMonitorService = systemMonitorService;
        _repositoryFactory = repositoryFactory;
        _configuration = configuration;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //添加计算机基础配置参数
        await AddOrUpdateComputerParamAsync();
        //CPU、显卡 名称
        // CPU、内存、磁盘 基本信息
        //TOP5 进程使用内存/CPU 情况
        //Window错误事件日志记录
        var delayMs = 1000 * 60;//1分钟更新一次资源
        await TaskHelper.CreateLongRunningTask(MonitorAsync, "SystemMonitorJob", delayMs, cancellationToken);
    }

    private async Task MonitorAsync()
    {
        try
        {
            await _systemMonitorService.RefreshAsync();

            //添加更新计算机资源信息监控情况
            await AddOrUpdateComputerMonitorAsync();
            //添加系统错误日志到监控中
            await AddSysErrorLogAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private async Task AddOrUpdateComputerMonitorAsync()
    {
        var topProcessList = await _systemMonitorService.GetTopProcessesAsync(5);
        var computerName = Environment.MachineName;
        var flowName = _configuration.GetLocalConfigOption().FlowName ?? "";
        using var repo = _repositoryFactory.CreateRepository<SysComputerMonitor>();
        var entity = await repo.FetchLatestAsync(x => x.FlowName == flowName);
        if (entity == null)
        {
            entity = new SysComputerMonitor()
            {
                ComputerName = computerName,
                FlowName = flowName,
                IP = GetLocalIp(),
                MemUsage = $"{_systemMonitorService.GetMemUsage():P2}",
                CpuUsage = $"{_systemMonitorService.GetCpuUsage():P2}",
                DiskUsage = _systemMonitorService.GetDiskUsage().ToJson(),
                TopN = JsonHelper.SerializeObject(topProcessList),
            };
            await repo.InsertAsync(entity);
        }
        else
        {
            entity.IP = GetLocalIp();
            entity.MemUsage = $"{_systemMonitorService.GetMemUsage():P2}";
            entity.CpuUsage = $"{_systemMonitorService.GetCpuUsage():P2}";
            entity.TopN = JsonHelper.SerializeObject(topProcessList);
            entity.DiskUsage = _systemMonitorService.GetDiskUsage().ToJson();
            await repo.UpdateAsync(entity);
        }
        _logger.LogInformation(JsonHelper.SerializeObject(entity));
    }

    private async Task AddOrUpdateComputerParamAsync()
    {
        var computerName = Environment.MachineName;
        var flowName = _configuration.GetLocalConfigOption().FlowName ?? "";
        var currentVersion = GetCurrentVersion();
        var param = new SysComputerParam()
        {
            FlowName = flowName,
            ComputerName = computerName,
            IP = GetLocalIp(),
            CpuName = _systemMonitorService.GetCpuName(),
            GpuDisplayName = _systemMonitorService.GetGpuDisplayName(),
            MemTotalSize = Math.Round(_systemMonitorService.GetTotalMemGb(), 2),
            DiskInfo = JsonHelper.SerializeObject(_systemMonitorService.GetDiskInfo()),
            CurrentVersion = currentVersion,
        };

        using var repo = _repositoryFactory.CreateRepository<SysComputerParam>();
        var entity = await repo.FetchLatestAsync(x => x.FlowName == flowName);
        if (entity == null)
        {
            entity = param;
            await repo.InsertAsync(entity);
        }
        else
        {
            entity.CurrentVersion = currentVersion;
            entity.IP = GetLocalIp();
            await repo.UpdateAsync(entity);
        }
    }

    private static string GetCurrentVersion()
    {
        var filename = "version";
        if (!File.Exists(filename))
        {
            File.WriteAllText(filename, "1.0");
        }
        return File.ReadAllText(filename);
    }

    private async Task AddSysErrorLogAsync()
    {
        var startTime = DateTime.Now.AddHours(-0.5);
        //检查最新0.5小时的日志
        var eventLogs = _systemMonitorService.GetEventLogEntries(startTime);
        if (eventLogs.Count == 0) return;

        //只记录崩溃；错误；  日志
        eventLogs = eventLogs.FindAll(x => x.Level.HasValue && x.Level.Value < 3);

        var machine = _configuration.GetCurrentMachine();
        var flowName = machine.FlowName == null ? "Unknown" : machine.FlowName;
        //查询数据表是否已插入该数据
        var fromDbLogs = await _eventLogService.QueryAsync(EventLogType.SystemErrorLog, startTime, DateTime.Now);
        fromDbLogs = fromDbLogs.FindAll(l => l.FlowName == flowName);
        if (fromDbLogs.Count == 0)
        {
            foreach (var log in eventLogs)
            {
                await _eventLogService.LogEventAsync("admin", EventLogType.SystemErrorLog, JsonHelper.SerializeObject(log));
            }
        }
        else
        {
            //如果数据库数据已经存在，则不插入
            foreach (var log in eventLogs)
            {
                var jsonLog = JsonHelper.SerializeObject(log);
                if (fromDbLogs.Exists(l => l.Message == jsonLog)) continue;
                await _eventLogService.LogEventAsync("admin", EventLogType.SystemErrorLog, jsonLog);
            }
        }
    }

    public static string GetLocalIp()
    {
        return GetLocalIpList().FirstOrDefault((string ip) => ip.StartsWith("192.168.10.")) ?? "";
    }

    public static List<string> GetLocalIpList()
    {
        return (from ip in Dns.GetHostAddresses(Dns.GetHostName())
                where ip.AddressFamily == AddressFamily.InterNetwork
                select ip.ToString()).ToList();
    }
}
