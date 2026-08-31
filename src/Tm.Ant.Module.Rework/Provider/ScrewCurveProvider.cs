using System.Collections.Concurrent;
using System.IO;

using Tm.Ant.Module.Rework.FlowServices;

namespace Tm.Ant.Module.Rework.Provider;
public interface IScrewCurveProvider : ISingleton
{
    Task SetCurveAsync(ScrewCurveEvent screwCurveEvent);
    Dictionary<int, ScrewCurveEvent> GetCurveFromMemory();
}

internal class ScrewCurveProvider : IScrewCurveProvider
{
    private readonly ConcurrentDictionary<int, ScrewCurveEvent> _dictionary = new();
    private readonly IConfiguration _configuration;
    public ScrewCurveProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    private async Task SaveAsync(ScrewCurveEvent @event)
    {
        var model = @event.ScrewCurve;
        var productCode = @event.ProductCode;
        var cave = @event.Cave;

        //按日期为文件夹来存储曲线数据
        var dir = Path.Combine(_configuration.GetDataDirectory(), "screw", $"{DateTime.Now:yyyy-MM-dd}");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        //0-产品码；1-穴位；2-螺丝号；3-螺丝批id；4-时间
        var fileName = Path.Combine(dir, $"{productCode}_{cave}_{model.ScrewNum}_{model.ScrewId}_{DateTime.Now:HHmmss}.txt");
        await File.WriteAllTextAsync(fileName, JsonHelper.SerializeObject(model), System.Text.Encoding.UTF8);
    }
    public async Task SetCurveAsync(ScrewCurveEvent screwCurveEvent)
    {
        var key = screwCurveEvent.ScrewCurve.ScrewId;
        _dictionary[key] = screwCurveEvent;
        await SaveAsync(screwCurveEvent);
    }

    public Dictionary<int, ScrewCurveEvent> GetCurveFromMemory()
    {
        if (_dictionary.Count == 0)
        {
            InitNewestFile();
        }
        return _dictionary.ToDictionary(k => k.Key, k => k.Value);
    }

    private void InitNewestFile()
    {
        var dir = Path.Combine(_configuration.GetDataDirectory(), "screw");
        if (!Directory.Exists(dir))
        {
            return;
        }
        try
        {
            var directory = new DirectoryInfo(dir);
            var subDirectorys = directory.GetDirectories();
            var firstDic = subDirectorys.OrderByDescending(d => d.LastWriteTime).First();
            if (firstDic == null) return;
            var file = firstDic.GetFiles()?.OrderByDescending(t => t.LastWriteTime).First();
            if (file == null) return;
            var json = System.IO.File.ReadAllText(file.FullName);
            var e = JsonHelper.DeserializeObject<ScrewCurveEvent>(json);
            _dictionary[e.ScrewCurve.ScrewId] = e;
        }
        catch { }
    }
}

