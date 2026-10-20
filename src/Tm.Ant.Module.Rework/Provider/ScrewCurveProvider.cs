using System.Collections.Concurrent;
using System.IO;

namespace Tm.Ant.Module.Rework.Provider;

/// <summary>
/// 本地曲线文件和最近曲线快照，不访问数据库。
/// </summary>
public interface IScrewCurveProvider : ISingleton
{
    Task SetCurveAsync(ScrewCurveEvent item, CancellationToken token = default);
    Dictionary<int, ScrewCurveEvent> GetCurveFromMemory();
    Task LoadLatestAsync(CancellationToken token);
    Task<IReadOnlyList<ScrewCurveEvent>> QueryAsync(DateTime date, string code, CancellationToken token);
}

internal sealed class ScrewCurveProvider(IConfiguration configuration, ILogger<ScrewCurveProvider> logger)
    : IScrewCurveProvider
{
    private readonly ConcurrentDictionary<int, ScrewCurveEvent> _latest = new();
    private string DataRoot => configuration["ReworkCurveDirectory"]
        ?? Path.Combine(AppContext.BaseDirectory, "Data", "screw");

    public async Task SetCurveAsync(ScrewCurveEvent item, CancellationToken token = default)
    {
        var directory = Path.Combine(DataRoot, DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(directory);
        var code = string.Concat(item.ProductCode.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var name = $"{code}_{item.Cave}_{item.ScrewCurve.ScrewNum}_{item.ScrewCurve.ScrewId}_{DateTime.Now:HHmmssfffffff}.txt";
        await File.WriteAllTextAsync(Path.Combine(directory, name), JsonConvert.SerializeObject(item), token);
        _latest[item.ScrewCurve.ScrewId] = item;
    }

    public Dictionary<int, ScrewCurveEvent> GetCurveFromMemory()
    {
        return _latest.ToDictionary();
    }

    public async Task LoadLatestAsync(CancellationToken token)
    {
        if (!Directory.Exists(DataRoot))
        {
            return;
        }

        var directory = Directory.EnumerateDirectories(DataRoot).OrderDescending().FirstOrDefault();
        if (directory == null)
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.txt").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            token.ThrowIfCancellationRequested();
            if (_latest.Count >= 2)
            {
                break;
            }

            try
            {
                var item = JsonConvert.DeserializeObject<ScrewCurveEvent>(await File.ReadAllTextAsync(file, token));
                if (item?.ScrewCurve != null)
                {
                    _latest.TryAdd(item.ScrewCurve.ScrewId, item);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "恢复最近曲线时跳过无效文件({File})", file);
            }
        }
    }

    public async Task<IReadOnlyList<ScrewCurveEvent>> QueryAsync(DateTime date, string code, CancellationToken token)
    {
        var directory = Path.Combine(DataRoot, date.ToString("yyyy-MM-dd"));
        var items = new List<ScrewCurveEvent>();
        if (!Directory.Exists(directory))
        {
            return items;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.txt").Order())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var item = JsonConvert.DeserializeObject<ScrewCurveEvent>(await File.ReadAllTextAsync(file, token));
                if (item != null && item.ProductCode == code)
                {
                    items.Add(item);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "曲线文件({File})无法解析，跳过此文件", file);
            }
        }

        return items;
    }
}
