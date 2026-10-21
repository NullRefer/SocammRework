namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 按设备ID读码和读写PLC；写入在此层最多尝试三次。
/// </summary>
public interface IFlowIoService : ISingleton
{
    /// <summary>
    /// 按设备ID扫码并裁剪两端控制字符和空白，校验产品码后返回，由Flow执行后续交互。
    /// </summary>
    Task<OperationResult<string>> ScanAsync(string scannerId, CancellationToken token);
    /// <summary>
    /// 按主PLC设备ID读取INT，读取失败的默认值由工站按业务要求校验。
    /// </summary>
    Task<OperationResult<short>> ReadShortAsync(string address, CancellationToken token);
    /// <summary>
    /// 按主PLC设备ID读取30字符产品码并裁剪两端控制字符和空白，读取失败保留原结果。
    /// </summary>
    Task<OperationResult<string>> ReadCodeAsync(string address, CancellationToken token);
    /// <summary>
    /// 向主PLC写入INT、STRING或INT数组，最多三次尝试并记录最终结果。
    /// </summary>
    Task<OperationResult> WriteAsync(string address, object value, CancellationToken token);
}
