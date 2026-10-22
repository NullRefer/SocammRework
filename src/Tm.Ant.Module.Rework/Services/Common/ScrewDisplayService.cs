using System.Collections.ObjectModel;

using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class ScrewDisplayService : IScrewDisplayService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScrewDisplayService> _logger;
    private ScrewDisplaySnapshot _latest;

    public ScrewDisplayService(IEventBus eventBus, ILogger<ScrewDisplayService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public ScrewDisplaySnapshot Latest => Volatile.Read(ref _latest);

    public void Publish(string productCode, DateTime time, IReadOnlyDictionary<int, string> caveResults)
    {
        var results = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>(caveResults));
        var snapshot = new ScrewDisplaySnapshot(productCode, time, time, results);
        Interlocked.Exchange(ref _latest, snapshot);
        try
        {
            _eventBus.Publish(new ScrewDisplayChangedEvent { Snapshot = snapshot });
        }
        catch (Exception ex)
        {
            // 展示通知失败不改变产品上传或软下线流程，最新快照仍可在页面进入时读取。
            _logger.LogError(ex, "螺丝显示通知发布失败，产品({Code})", productCode);
        }
    }
}
