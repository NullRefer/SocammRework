using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Interfaces.Flows;
using Tm.Ant.Module.Rework.Models.Pto;
using Tm.Ant.Module.Rework.Models.SFC;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Services.Flows;

internal sealed class UnloadService(IFlowIoService io, IProductService products, INvdSfcService sfc,
    IEventBus events, IFlowResultState state, ILogger<UnloadService> logger) : IUnloadService
{
    public async Task<OperationResult> ScanAsync(UnloadAddress address, string scannerId, CancellationToken token)
    {
        try
        {
            var scan = await io.ScanAsync(scannerId, token);
            if (!scan.IsSuccess)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料扫码失败：{scan.Message}", scan.Code);
            }

            var pendingDisplay = new FlowEventPubSub { ProductCode = scan.Data, IsOk = false };
            state.Set(pendingDisplay);
            events.Publish(pendingDisplay);
            var entity = await products.GetFlowAsync(scan.Data, token);
            if (entity == null)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料查询失败，产品({scan.Data})：未找到在线生产记录。");
            }

            var results = entity.GetFinalResult();
            var isOk = results.Count > 0 && results.Values.All(value => value == "OK");
            var display = new FlowEventPubSub { ProductCode = scan.Data, CaveResults = results, IsOk = isOk };
            state.Set(display);
            events.Publish(display);
            var sfcDescription = "质量NG，未上传PASS";
            if (isOk)
            {
                var pass = await sfc.StationRecordAsync(new StationRecordRequest
                {
                    Sn = scan.Data,
                    Result = "PASS",
                    ErrorCode = "N/A"
                }, token);
                if (!pass.IsSuccess)
                {
                    await io.WriteAsync(address.Feedback, (short)2, token);
                    return OperationResult.Error($"下料SFC过站失败，产品({scan.Data})：{pass.Result}", pass.Rc);
                }

                sfcDescription = "PASS接口处理成功（屏蔽时模拟成功）";
            }

            var offline = await products.OfflineFlowAsync(entity, token);
            if (!offline.IsSuccess)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"下料软删除失败：{offline.Message}", offline.Code);
            }

            await io.WriteAsync(address.ProductCode, scan.Data, token);
            // D201仍为处理成功反馈，产品质量单独展示，不擅自改变已确认的协议含义。
            await io.WriteAsync(address.Feedback, (short)1, token);
            logger.LogInformationToUI("下料完成，产品({Code})，需拆({Required})，逐穴结果({Results})，总质量({Quality})，SFC({Sfc})，主表已软下线",
                LogCategory.Comm, scan.Data, entity.UnScrewCaves, JsonConvert.SerializeObject(results),
                isOk ? "OK" : "NG", sfcDescription);
            return OperationResult.Success();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"下料交互异常：{ex.Message}");
        }
    }
}
