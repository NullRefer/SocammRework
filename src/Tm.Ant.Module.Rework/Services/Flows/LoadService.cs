using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Interfaces.Flows;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Services.Flows;

internal sealed class LoadService(IFlowIoService io, INvdSfcService sfc, IProductService products,
    ILogger<LoadService> logger) : ILoadService
{
    public async Task<OperationResult> ScanAsync(LoadAddress address, string scannerId, CancellationToken token)
    {
        try
        {
            var scan = await io.ScanAsync(scannerId, token);
            if (!scan.IsSuccess)
            {
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"上料扫码失败：{scan.Message}", scan.Code);
            }

            var response = await sfc.GetCavesAsync(scan.Data, token);
            if (!response.IsSuccess)
            {
                await io.WriteAsync(address.ProductCode, new string('\0', Consts.PRODUCTCODELEN), token);
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"上料SFC查询失败，产品({scan.Data})：{response.Result}", response.Rc);
            }

            var saved = await products.AddFlowAsync(scan.Data, response.DoCaves, response.Locations, token);
            if (!saved.IsSuccess)
            {
                await io.WriteAsync(address.ProductCode, new string('\0', Consts.PRODUCTCODELEN), token);
                await io.WriteAsync(address.Feedback, (short)2, token);
                return OperationResult.Error($"上料建档失败：{saved.Message}", saved.Code);
            }

            await io.WriteAsync(address.ProductCode, scan.Data, token);
            await io.WriteAsync(address.Feedback, (short)1, token);
            logger.LogInformationToUI("上料完成，产品({Code})，SFC位置({Locations})，需拆({Caves})，建档ID({Id})",
                LogCategory.Comm, scan.Data, response.Locations, saved.Data.UnScrewCaves, saved.Data.Id);
            return OperationResult.Success();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await io.WriteAsync(address.Feedback, (short)2, token);
            return OperationResult.Error($"上料交互异常：{ex.Message}");
        }
    }
}
