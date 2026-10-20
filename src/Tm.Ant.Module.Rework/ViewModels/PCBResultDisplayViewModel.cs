using System.IO;

using Tm.Ant.Framework.Models.Events;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class PCBResultDisplayViewModel(IEventBus events, IFlowResultState state)
    : ObservableObject, INavigationAware
{
    private FlowEventPubSub _subscription;
    [ObservableProperty] private string _productCode = "";
    [ObservableProperty] private string _totalResult = "";
    [ObservableProperty] private ObservableCollection<CaveMarker> _markers = new();

    public void OnNavigatedTo(object parameter)
    {
        _subscription?.Unsubscribe(OnResult);
        _subscription = events.Subscribe<FlowEventPubSub>(OnResult, ThreadOption.UIThread);
        if (state.Latest != null)
        {
            OnResult(state.Latest);
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
