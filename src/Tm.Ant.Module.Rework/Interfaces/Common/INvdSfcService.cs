namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 查询SFC需拆位置和上传PASS过站，保留原接口报文。
/// </summary>
public interface INvdSfcService : ISingleton
{
    /// <summary>
    /// 请求并校验SFC位置，返回八穴标记；屏蔽时使用模拟标记。
    /// </summary>
    Task<NvdReworkResponse> GetCavesAsync(string code, CancellationToken token = default);
    /// <summary>
    /// 上传SFC过站报文，屏蔽时返回模拟成功；保留接口失败原因。
    /// </summary>
    Task<PassResponse> StationRecordAsync(RecoredRequest request, CancellationToken token = default);
}
