namespace Tm.Ant.Module.Rework.Interfaces.Common;

/// <summary>
/// 按设备ID读码和读写PLC；写入在此层最多尝试三次。
/// </summary>
public interface IFlowIoService : ISingleton
{
    /// <summary>
    /// 执行本工站扫码、校验和业务处理，按协议回写成功或失败，由调用入口随后清零扫码触发。
    /// </summary>
    Task<OperationResult<string>> ScanAsync(string scannerId, CancellationToken token);
    /// <summary>
    /// 按主PLC设备ID读取INT，读取失败的默认值由工站按业务要求校验。
    /// </summary>
    Task<OperationResult<short>> ReadShortAsync(string address, CancellationToken token);
    /// <summary>
    /// 按主PLC设备ID读取30字符产品码，读取失败由业务空值校验处理。
    /// </summary>
    Task<OperationResult<string>> ReadCodeAsync(string address, CancellationToken token);
    /// <summary>
    /// 向主PLC写入INT、STRING或INT数组，最多三次尝试并记录最终结果。
    /// </summary>
    Task<OperationResult> WriteAsync(string address, object value, CancellationToken token);
}
