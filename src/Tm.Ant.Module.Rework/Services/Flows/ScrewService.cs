using Tm.Ant.Communication.Tightening.Abstractions.Models;
using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Interfaces.Flows;
using Tm.Ant.Module.Rework.Models.Pto;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Services.Flows;

internal sealed class ScrewService(IFlowIoService io, IProductService products, IScrewCurveProvider curves,
    IEventBus events, ILogger<ScrewService> logger) : IScrewService
{
    public async Task<OperationResult> ScanAsync(ScrewAddress.ScanGroup address, string scannerId,
        CancellationToken token)
    {
        try
        {
            var scan = await io.ScanAsync(scannerId, token);
            if (!scan.IsSuccess)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"拆螺丝扫码失败：{scan.Message}", scan.Code);
            }

            var entity = await products.GetFlowAsync(scan.Data, token);
            if (entity == null)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"拆螺丝查询失败，产品({scan.Data})：无在线上料记录，未下发加工数据。");
            }

            var required = entity.GetRequiredCaves();
            await io.WriteAsync(address.RequiredCaves, required, token);
            await io.WriteAsync(address.ProductCode, scan.Data, token);
            await io.WriteAsync(address.Updated, (short)1, token);
            await io.WriteAsync(address.Feedback, (short)1, token);
            logger.LogInformationToUI("拆螺丝扫码完成，产品({Code})，在线记录({Id})，下发需拆({Caves})、更新标记及扫码成功",
                LogCategory.Comm, scan.Data, entity.Id, entity.UnScrewCaves);
            return OperationResult.Success();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"拆螺丝扫码交互异常：{ex.Message}");
        }
    }

    public async Task<OperationResult> SaveResultAsync(ScrewAddress.ResultGroup address, CancellationToken token)
    {
        var result = (await io.ReadShortAsync(address.Trigger, token)).Data;
        if (result == 0)
        {
            return OperationResult.Success();
        }

        logger.LogInformationToUI("穴位结果触发，地址({Address})，值({Value})", LogCategory.Comm, address.Trigger, result);
        var code = (await io.ReadCodeAsync(address.ProductCode, token)).Data?.Trim('\0', ' ');
        var cave = (await io.ReadShortAsync(address.Cave, token)).Data;
        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult.Error($"保存穴位结果失败，地址({address.ProductCode})：产品码为空，保留结果触发。");
        }

        var saved = await products.UpdateFlowAsync(code, cave, result, token);
        if (!saved.IsSuccess)
        {
            return OperationResult.Error($"保存穴位结果失败：{saved.Message}，保留结果触发。", saved.Code);
        }

        await io.WriteAsync(address.Trigger, (short)0, token);
        logger.LogInformationToUI("穴位拆除结果已保存，产品({Code})，穴位({Cave})，质量({Quality})，已执行清零响应",
            LogCategory.Comm, code, cave, result == 1 ? "OK" : "NG");
        return OperationResult.Success();
    }

    public async Task SaveCurveAsync(ScrewAddress.CurveGroup[] groups, int toolNo, TighteningCurve curve,
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
                var plcTool = (await io.ReadShortAsync(screw.ToolNo, token)).Data;
                if (plcTool != toolNo)
                {
                    continue;
                }

                var code = (await io.ReadCodeAsync(group.ProductCode, token)).Data?.Trim('\0', ' ');
                var cave = (await io.ReadShortAsync(group.Cave, token)).Data;
                var program = (await io.ReadShortAsync(screw.Program, token)).Data;
                if (string.IsNullOrWhiteSpace(code) || cave is < 1 or > 8)
                {
                    continue;
                }

                var item = new ScrewCurveEvent
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
                await curves.SetCurveAsync(item, token);
                events.Publish(item);
                logger.LogInformationToUI("本地曲线保存完成，产品({Code})，穴位({Cave})，螺丝({Screw})，螺丝批({Tool})，点数({Count})",
                    LogCategory.Comm, code, cave, screw.Number, toolNo, curve.Torques.Count);
                return;
            }
        }

        var unassociated = new ScrewCurveEvent
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
        await curves.SetCurveAsync(unassociated, token);
        logger.LogWarningToUI("螺丝批({Tool})曲线未匹配有效产品/穴位，原始曲线已保存为UNMATCHED文件，不写业务数据库",
            LogCategory.Comm, toolNo);
    }
}
