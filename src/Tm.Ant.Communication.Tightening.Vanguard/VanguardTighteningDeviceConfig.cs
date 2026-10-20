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

    [LocalizedDisplayName(typeof(VanguardResources), nameof(VanguardResources.Ip), "IP地址")]
    public string Ip { get; set; } = "";

    [LocalizedDisplayName(typeof(VanguardResources), nameof(VanguardResources.Port), "端口")]
    public int Port { get; set; } = 4545;

    [LocalizedDisplayName(typeof(VanguardResources), nameof(VanguardResources.ToolNo), "螺丝批编号")]
    public int ToolNo { get; set; } = 1;

    [LocalizedDisplayName(typeof(VanguardResources), nameof(VanguardResources.Timeout), "通讯超时(ms)")]
    public int TimeoutMs { get; set; } = 1000;
}

/// <summary>
/// 设备管理属性名称的资源入口。
/// </summary>
public static class VanguardResources
{
    public static string Ip => "IP地址";
    public static string Port => "端口";
    public static string ToolNo => "螺丝批编号";
    public static string Timeout => "通讯超时(ms)";
}
