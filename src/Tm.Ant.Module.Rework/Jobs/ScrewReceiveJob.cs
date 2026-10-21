using Tm.Ant.Communication.Tightening.Abstractions.Interfaces;
using Tm.Ant.Communication.Tightening.Abstractions.Models;
using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.Pto;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Jobs;

/// <summary>
/// 独立接收两把拧紧枪的结果与曲线，关联PLC并保存本地曲线文件。
/// </summary>
internal sealed class ScrewReceiveJob : TmJobBase
{
    private readonly IDeviceFactory _deviceFactory;
    private readonly IFlowIoService _io;
    private readonly IScrewCurveProvider _screwCurveProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScrewReceiveJob> _logger;
    private readonly ScrewAddress _address = new();

    public ScrewReceiveJob(IDeviceFactory deviceFactory, IFlowIoService io, IScrewCurveProvider screwCurveProvider,
        IConfiguration configuration, ILogger<ScrewReceiveJob> logger)
    {
        _deviceFactory = deviceFactory;
        _io = io;
        _screwCurveProvider = screwCurveProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var flowName = _configuration.GetLocalConfigOption().FlowName;
        if (flowName is not ("UNSCREW1" or "UNSCREW2"))
        {
            return;
        }

        if (!_deviceFactory.TryGetDeviceService<IPlcService>(DeviceIds.MainPlc, out _))
        {
            _logger.LogWarningToUI("拧紧接收任务未启动，PLC设备({DeviceId})未配置", LogCategory.Comm, DeviceIds.MainPlc);
            return;
        }

        if (!_deviceFactory.TryGetDeviceService<ITighteningDevice>(DeviceIds.Tightening1, out var first)
            || !_deviceFactory.TryGetDeviceService<ITighteningDevice>(DeviceIds.Tightening2, out var second))
        {
            _logger.LogWarningToUI("拧紧接收任务未启动，两把拧紧枪尚未全部配置", LogCategory.Comm);
            return;
        }

        Func<TighteningResult, Task> firstResult = result => ReceiveResultAsync(1, result, cancellationToken);
        Func<TighteningResult, Task> secondResult = result => ReceiveResultAsync(2, result, cancellationToken);
        Func<TighteningCurve, Task> firstCurve = curve => SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 1, curve, cancellationToken);
        Func<TighteningCurve, Task> secondCurve = curve => SaveCurveAsync(
            [_address.Curve1, _address.Curve2], 2, curve, cancellationToken);
        try
        {
            first.TighteningResultReceivedAsync += firstResult;
            second.TighteningResultReceivedAsync += secondResult;
            first.TighteningCurveReceivedAsync += firstCurve;
            second.TighteningCurveReceivedAsync += secondCurve;
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            first.TighteningResultReceivedAsync -= firstResult;
            second.TighteningResultReceivedAsync -= secondResult;
            first.TighteningCurveReceivedAsync -= firstCurve;
            second.TighteningCurveReceivedAsync -= secondCurve;
        }
    }

    private Task ReceiveResultAsync(int toolNo, TighteningResult result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (result.ToolNo != toolNo)
        {
            _logger.LogWarningToUI("拧紧结果接收终止，设备({DeviceId})，工具号({Actual})与预期({Expected})不一致",
                LogCategory.Comm, result.DeviceId, result.ToolNo, toolNo);
            return Task.CompletedTask;
        }

        _logger.LogInformationToUI(
            "接收拧紧结果，设备({DeviceId})，批号({Tool})，程序({Program})，质量({Result})，扭矩({Torque})，圈数({Turns})，耗时({Duration})",
            LogCategory.Comm, result.DeviceId, toolNo, result.ProgramName, result.ResultCode,
            result.Torque, result.Turns, result.Duration);
        return Task.CompletedTask;
    }

    private async Task SaveCurveAsync(ScrewAddress.CurveGroup[] groups, int toolNo, TighteningCurve curve,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (curve.ToolNo != toolNo)
        {
            _logger.LogWarningToUI("拧紧曲线接收终止，设备({DeviceId})，工具号({Actual})与预期({Expected})不一致",
                LogCategory.Comm, curve.DeviceId, curve.ToolNo, toolNo);
            return;
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

                var code = (await _io.ReadCodeAsync(group.ProductCode, token)).Data;
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
                await _screwCurveProvider.SetCurveAsync(item, token);
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
        await _screwCurveProvider.SetCurveAsync(unassociated, token);
        _logger.LogWarningToUI("螺丝批({Tool})曲线未匹配有效产品/穴位，原始曲线已保存为UNMATCHED文件，不写业务数据库",
            LogCategory.Comm, toolNo);
    }
}
