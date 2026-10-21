namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 封装SFC接口调用、响应判定与位置解析，调用方只消费业务操作结果。
/// </summary>
public interface INvdSfcService : ISingleton
{
    /// <summary>
    /// 执行SFC上料确认，按站点和成对L/J位置返回八穴标记；无位置或不成对的穴位不拆，屏蔽时使用模拟标记。
    /// </summary>
    Task<OperationResult<(short[] Caves, string Locations)>> CheckAsync(string code,
        CancellationToken token = default);

    /// <summary>
    /// 为产品上传PASS过站，封装请求字段与响应判定；屏蔽时返回模拟成功。
    /// </summary>
    Task<OperationResult> StationRecordAsync(string code, CancellationToken token = default);
}
