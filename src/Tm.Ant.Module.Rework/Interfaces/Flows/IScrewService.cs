using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Interfaces.Flows;

/// <summary>
/// Screw工站的业务交互契约，设备ID由Flow传入。
/// </summary>
public interface IScrewService : ISingleton
{
    /// <summary>
    /// 执行本工站扫码、校验和业务处理，按协议回写成功或失败，由调用入口随后清零扫码触发。
    /// </summary>
    Task<OperationResult> ScanAsync(ScrewAddress.ScanGroup address, string scannerId, CancellationToken token);
    /// <summary>
    /// 接收穴位最终质量并保存主表，仅保存成功后清零结果地址，失败保留触发。
    /// </summary>
    Task<OperationResult> SaveResultAsync(ScrewAddress.ResultGroup address, CancellationToken token);
    /// <summary>
    /// 读取两套只读曲线关联地址并保存本地文件，不清零PLC、不写业务主表。
    /// </summary>
    Task SaveCurveAsync(ScrewAddress.CurveGroup[] groups, int toolNo,
        Tm.Ant.Communication.Tightening.Abstractions.Models.TighteningCurve curve, CancellationToken token);
}
