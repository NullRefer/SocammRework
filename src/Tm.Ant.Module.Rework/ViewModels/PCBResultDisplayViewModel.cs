using System.IO;

using Tm.Ant.Framework.Models.Events;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class PCBResultDisplayViewModel : ObservableObject, INavigationAware
{
    private readonly IEventBus _events;
    private readonly IFlowResultState _state;
    private FlowEventPubSub _subscription;

    [ObservableProperty]
    public partial string ProductCode { get; set; } = "";

    [ObservableProperty]
    public partial string TotalResult { get; set; } = "";

    [ObservableProperty]
    public partial ObservableCollection<CaveMarker> Markers { get; set; } = new();

    public PCBResultDisplayViewModel(IEventBus events, IFlowResultState state)
    {
        _events = events;
        _state = state;
    }

    public void OnNavigatedTo(object parameter)
    {
        _subscription?.Unsubscribe(OnResult);
        _subscription = _events.Subscribe<FlowEventPubSub>(OnResult, ThreadOption.UIThread);
        if (_state.Latest != null)
        {
            OnResult(_state.Latest);
        }
    }

    public void OnNavigatedFrom()
    {
        _subscription?.Unsubscribe(OnResult);
        _subscription = null;
    }

    private void OnResult(FlowEventPubSub result)
    {
        ProductCode = result.ProductCode;
        TotalResult = result.IsOk ? "OK" : "NG";
        var file = Path.Combine(AppContext.BaseDirectory, "Images", "RectangleData.json");
        var positions = JsonConvert.DeserializeObject<List<CavePosition>>(File.ReadAllText(file)) ?? new();
        Markers = new ObservableCollection<CaveMarker>(result.CaveResults.Select(pair =>
        {
            var position = positions.FirstOrDefault(item => item.Index == pair.Key) ?? new CavePosition();
            return new CaveMarker(pair.Key, pair.Value, position.RelativeX * 1000, position.RelativeY * 556.3612,
                position.RelativeWidth * 1000, position.RelativeHeight * 556.3612);
        }));
    }
}

public sealed record CaveMarker(int Cave, string Result, double X, double Y, double Width, double Height);

public sealed class CavePosition
{
    public int Index { get; set; }
    public double RelativeX { get; set; }
    public double RelativeY { get; set; }
    public double RelativeWidth { get; set; }
    public double RelativeHeight { get; set; }
}
