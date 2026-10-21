namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 封装SFC接口调用、响应判定与位置解析，调用方只消费业务操作结果。
/// </summary>
public interface INvdSfcService : ISingleton
{
    /// <summary>
    /// 确认上料站点并解析需拆穴位，成功时返回八穴标记和原始位置；屏蔽时使用模拟标记。
    /// </summary>
    Task<OperationResult<(short[] Caves, string Locations)>> GetCavesAsync(string code,
        CancellationToken token = default);

    /// <summary>
    /// 为产品上传PASS过站，封装请求字段与响应判定；屏蔽时返回模拟成功。
    /// </summary>
    Task<OperationResult> StationRecordAsync(string code, CancellationToken token = default);
}
