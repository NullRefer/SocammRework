using System.Windows;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using Tm.Ant.CommonViewWpf.Helpers;
using Tm.Ant.Framework.Models.Events;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class ScrewChartViewModel : ObservableObject, INavigationAware
{
    private readonly IScrewCurveProvider _curves;
    private readonly IEventBus _events;
    private readonly IDialogService _dialogs;
    private readonly ILogger _logger;
    private ScrewCurveEvent _subscription;
    private CancellationTokenSource _session;
    private Task _loading = Task.CompletedTask;

    public ScrewChartViewModel(IScrewCurveProvider curves, IEventBus events, IDialogService dialogs,
        ILogger<ScrewChartViewModel> logger)
    {
        _curves = curves;
        _events = events;
        _dialogs = dialogs;
        _logger = logger;
    }

    [ObservableProperty] private DateTime _selectDate = DateTime.Today;
    [ObservableProperty] private int _chartColumns = 1;
    [ObservableProperty] private int _chartRows = 1;
    [ObservableProperty] private ObservableCollection<ScrewPlotModel> _charts = new();

    [RelayCommand]
    private async Task QueryCurveAsync(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            _dialogs.ShowWarning("请输入产品码。");
            return;
        }

        try
        {
            var token = _session?.Token ?? CancellationToken.None;
            var items = await _curves.QueryAsync(SelectDate, productCode.Trim(), token);
            if (!token.IsCancellationRequested)
            {
                Refresh(items);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "本地曲线查询失败");
            _dialogs.ShowError($"本地曲线查询失败：{ex.Message}");
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        _session?.Cancel();
        _session?.Dispose();
        _session = new CancellationTokenSource();
        _subscription?.Unsubscribe(OnCurve);
        _subscription = _events.Subscribe<ScrewCurveEvent>(OnCurve, ThreadOption.UIThread);
        _loading = RestoreAsync(_session.Token);
    }

    private async Task RestoreAsync(CancellationToken token)
    {
        try
        {
            await _curves.LoadLatestAsync(token);
            if (!token.IsCancellationRequested)
            {
                Refresh(_curves.GetCurveFromMemory().OrderBy(item => item.Key).Select(item => item.Value));
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "最近曲线恢复失败");
            _dialogs.ShowError($"最近曲线恢复失败：{ex.Message}");
        }
    }

    public void OnNavigatedFrom()
    {
        _session?.Cancel();
        _subscription?.Unsubscribe(OnCurve);
        _subscription = null;
    }

    private void OnCurve(ScrewCurveEvent item)
    {
        if (_session?.IsCancellationRequested == false)
        {
            Refresh(_curves.GetCurveFromMemory().OrderBy(pair => pair.Key).Select(pair => pair.Value));
        }
    }

    private void Refresh(IEnumerable<ScrewCurveEvent> items)
    {
        var plots = new List<ScrewPlotModel>();
        foreach (var item in items)
        {
            var data = item.ScrewCurve;
            var model = new PlotModel { Title = $"{item.ProductCode} | 穴{item.Cave} | 螺丝{data.ScrewNum} | 批{data.ScrewId}" };
            var series = new LineSeries();
            for (var i = 0; i < Math.Min(data.AngleList.Count, data.TorqueList.Count); i++)
            {
                series.Points.Add(new DataPoint(data.AngleList[i], data.TorqueList[i]));
            }

            model.Series.Add(series);
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "曲线采样坐标" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "扭矩（设备原始单位）" });
            PlotHelper.ApplyTheme(model);
            plots.Add(new ScrewPlotModel { PlotId = plots.Count + 1, PlotModel = model });
        }

        Charts = new ObservableCollection<ScrewPlotModel>(plots);
        ChartColumns = plots.Count <= 1 ? 1 : 2;
        ChartRows = Math.Max(1, (int)Math.Ceiling(plots.Count / (double)ChartColumns));
    }
}

public sealed class ScrewPlotModel
{
    public int PlotId { get; set; }
    public PlotModel PlotModel { get; set; }
}
