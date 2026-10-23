namespace Tm.Ant.Module.Rework.Provider;

/// <summary>
/// 按年月保存本地曲线文件。
/// </summary>
public interface IScrewCurveProvider : ISingleton
{
    /// <summary>
    /// 将曲线保存到框架数据根目录下的 screw/年月 目录；保存异常记录日志并返回false。
    /// </summary>
    Task<bool> SetCurveAsync(ScrewCurveData item, CancellationToken token = default);
}

internal sealed class ScrewCurveProvider : IScrewCurveProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScrewCurveProvider> _logger;

    public ScrewCurveProvider(IConfiguration configuration, ILogger<ScrewCurveProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SetCurveAsync(ScrewCurveData item, CancellationToken token = default)
    {
        try
        {
            var time = DateTime.Now;
            var directory = Path.Combine(_configuration.GetDataDirectory(), "screw", time.ToString("yyyy-MM"));
            Directory.CreateDirectory(directory);
            var code = string.Concat(item.ProductCode.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var name = $"{code}_{item.Cave}_{item.ScrewCurve.ScrewNum}_{item.ScrewCurve.ScrewId}_{time:yyyyMMddHHmmssfffffff}.txt";
            await File.WriteAllTextAsync(Path.Combine(directory, name), JsonConvert.SerializeObject(item), token);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogErrorToUI(ex, "本地曲线保存失败，产品({Code})，穴位({Cave})，螺丝({Screw})，螺丝批({Tool})，原因({Reason})",
                LogCategory.Comm, item?.ProductCode, item?.Cave, item?.ScrewCurve?.ScrewNum,
                item?.ScrewCurve?.ScrewId, ex.Message);
            return false;
        }
    }
}
