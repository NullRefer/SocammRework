using Tm.Ant.Communication.Infra.Models;
using Tm.Ant.Framework.Attributes;

namespace Tm.Ant.Communication.Tightening.Vanguard;

/// <summary>
/// VLib 螺丝批的 Modbus TCP 配置，由设备管理页面保存。
/// </summary>
public sealed class VanguardTighteningDeviceConfig : DeviceSubTypeConfigBase
{
    public VanguardTighteningDeviceConfig()
    {
        DeviceSubType = VanguardTighteningDevice.SubType;
    }

    /// <summary>
    /// 控制器的 Modbus TCP 通讯 IP 地址。
    /// </summary>
    [LocalizedCategory(typeof(LangResources), nameof(LangResources.DeviceConfig_Category_Network), "网络配置")]
    [LocalizedDisplayName(typeof(LangResources), nameof(LangResources.DeviceConfig_Ip), "IP地址")]
    [LocalizedDescription(typeof(LangResources), nameof(LangResources.DeviceConfig_Description_Ip),
        "VLib 控制器的 Modbus TCP 通讯 IP 地址。")]
    public string Ip { get; set; } = "";

    /// <summary>
    /// 控制器的 Modbus TCP 通讯端口。
    /// </summary>
    [LocalizedCategory(typeof(LangResources), nameof(LangResources.DeviceConfig_Category_Network), "网络配置")]
    [LocalizedDisplayName(typeof(LangResources), nameof(LangResources.DeviceConfig_Port), "端口")]
    [LocalizedDescription(typeof(LangResources), nameof(LangResources.DeviceConfig_Description_Port),
        "VLib 控制器的 Modbus TCP 通讯端口，范围为 1–65535。")]
    public int Port { get; set; } = 4545;

    /// <summary>
    /// 用于标识拧紧枪的编号，不参与业务关联校验。
    /// </summary>
    [LocalizedCategory(typeof(LangResources), nameof(LangResources.DeviceConfig_Category_Tightening), "拧紧配置")]
    [LocalizedDisplayName(typeof(LangResources), nameof(LangResources.DeviceConfig_ToolNo), "螺丝批编号")]
    [LocalizedDescription(typeof(LangResources), nameof(LangResources.DeviceConfig_Description_ToolNo),
        "用于标识拧紧枪的编号，当前业务不使用此配置进行关联或校验。")]
    public int ToolNo { get; set; } = 1;

    /// <summary>
    /// 连接及通讯读写超时毫秒数。
    /// </summary>
    [LocalizedCategory(typeof(LangResources), nameof(LangResources.DeviceConfig_Category_Tightening), "拧紧配置")]
    [LocalizedDisplayName(typeof(LangResources), nameof(LangResources.DeviceConfig_TimeoutMs), "通讯超时(ms)")]
    [LocalizedDescription(typeof(LangResources), nameof(LangResources.DeviceConfig_Description_TimeoutMs),
        "连接及通讯读写的超时时间，单位为毫秒，最小为 100。")]
    public int TimeoutMs { get; set; } = 1000;
}
