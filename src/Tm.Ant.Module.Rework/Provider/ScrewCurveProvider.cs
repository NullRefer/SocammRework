namespace Tm.Ant.Module.Rework.Provider;

/// <summary>
/// 按年月保存本地曲线文件。
/// </summary>
public interface IScrewCurveProvider : ISingleton
{
    /// <summary>
    /// 将曲线保存到框架数据根目录下的 screw/年月 目录。
    /// </summary>
    Task SetCurveAsync(ScrewCurveData item, CancellationToken token = default);
}

internal sealed class ScrewCurveProvider : IScrewCurveProvider
{
    private readonly IConfiguration _configuration;

    public ScrewCurveProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SetCurveAsync(ScrewCurveData item, CancellationToken token = default)
    {
        var time = DateTime.Now;
        var directory = Path.Combine(_configuration.GetDataDirectory(), "screw", time.ToString("yyyy-MM"));
        Directory.CreateDirectory(directory);
        var code = string.Concat(item.ProductCode.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var name = $"{code}_{item.Cave}_{item.ScrewCurve.ScrewNum}_{item.ScrewCurve.ScrewId}_{time:yyyyMMddHHmmssfffffff}.txt";
        await File.WriteAllTextAsync(Path.Combine(directory, name), JsonConvert.SerializeObject(item), token);
    }
}
