using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Interfaces.Flows;

/// <summary>
/// Load工站的业务交互契约，设备ID由Flow传入。
/// </summary>
public interface ILoadService : ISingleton
{
    /// <summary>
    /// 执行本工站扫码、校验和业务处理，按协议回写成功或失败，由调用入口随后清零扫码触发。
    /// </summary>
    Task<OperationResult> ScanAsync(LoadAddress address, string scannerId, CancellationToken token);
}
