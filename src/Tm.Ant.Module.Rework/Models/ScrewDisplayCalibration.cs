namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 螺丝显示的本机24点标定，空集合表示尚未标定。
/// </summary>
[LocalOptionConfig("ScrewDisplayCalibration", "螺丝显示标定", "按穴位顺序保存24颗螺丝的归一化坐标")]
public sealed class ScrewDisplayCalibration : IConfigNormalizer<ScrewDisplayCalibration>, IConfigValidator
{
    public const int PositionCount = 24;
    public List<ScrewCalibrationPoint> Points { get; set; } = [];

    public ScrewDisplayCalibration Normalize()
    {
        Points ??= [];
        return this;
    }

    public IReadOnlyList<string> Validate()
    {
        if (Points == null || Points.Count is not (0 or PositionCount))
        {
            return ["螺丝标定必须为空或恰好包含24个坐标。"];
        }

        if (Points.Any(point => !ScrewDisplayGeometry.IsValid(point)))
        {
            return ["螺丝标定坐标必须为图片范围内的有限数值。"];
        }

        return [];
    }
}

/// <summary>
/// 相对于PCB图片宽高的归一化螺丝坐标。
/// </summary>
public sealed record ScrewCalibrationPoint(double X, double Y);

/// <summary>
/// PCB原图坐标系及鼠标坐标换算，不依赖窗口大小。
/// </summary>
public static class ScrewDisplayGeometry
{
    public const double ImageWidth = 1517;
    public const double ImageHeight = 844;

    /// <summary>
    /// 判断坐标是否位于归一化图片范围内。
    /// </summary>
    public static bool IsValid(ScrewCalibrationPoint point)
    {
        return point != null && double.IsFinite(point.X) && double.IsFinite(point.Y)
            && point.X is >= 0 and <= 1 && point.Y is >= 0 and <= 1;
    }

    public static ScrewCalibrationPoint Normalize(double x, double y, double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
        {
            return null;
        }

        var point = new ScrewCalibrationPoint(x / width, y / height);
        return IsValid(point) ? point : null;
    }
}
