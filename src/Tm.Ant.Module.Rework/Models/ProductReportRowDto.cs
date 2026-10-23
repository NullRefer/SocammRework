using System.ComponentModel;

namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 产品报表行及导出列，不向页面暴露生产实体。
/// </summary>
public sealed class ProductReportRowDto
{
    [Description("ID")]
    public int Id { get; init; }

    [Description("产品码")]
    public string ProductCode { get; init; } = "";

    [Description("需拆标记")]
    public string UnScrewCaves { get; init; } = "";

    [Description("位置")]
    public string Locations { get; init; } = "";

    [Description("穴位结果")]
    public string ResultJson { get; init; } = "";

    [Description("下线状态")]
    public string Status { get; init; } = "";

    [Description("上料时间")]
    public DateTime CreateTime { get; init; }

    [Description("更新时间")]
    public DateTime UpdateTime { get; init; }
}
