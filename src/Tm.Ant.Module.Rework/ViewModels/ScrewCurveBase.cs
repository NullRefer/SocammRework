using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

using Tm.Ant.CommonViewWpf.Utils;
using Tm.Ant.Module.Rework.FlowServices;

namespace Tm.Ant.Module.Rework.ViewModels;
public partial class ScrewCurveBase : ObservableObject
{
    [ObservableProperty]
    private int _chartColumns = 1;

    [ObservableProperty]
    private int _chartRows = 1;

    [ObservableProperty]
    private ObservableCollection<ScrewPlotModel> _charts = new();

    public readonly IConfiguration _configuration;
    public ScrewCurveBase(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public void RefreshPlots(Dictionary<int, ScrewCurveEvent> data)
    { 
        foreach (var (key, value) in data)
        {
            var plotModel = CreateScrewModel(value);
            var chartModel = new ScrewPlotModel
            {
                PlotId = key,
                PlotModel = plotModel
            };

            this.UIInvoke(() =>
            {
                var exist = Charts.FirstOrDefault(c => c.PlotId == key);
                if (exist != null)
                {
                    Charts.Remove(exist);
                }
                Charts.Add(chartModel);

                ChartColumns = Charts.Count == 1 ? 1 : 2;
                ChartRows = Charts.Count <= 2 ? 1 : (int)Math.Ceiling(Charts.Count / 2.0);
            });
        }
    }
    private static PlotModel CreateScrewModel(ScrewCurveEvent @event)
    {

        //double lsl = commConfig.Min;
        //double usl = commConfig.Max;

        var curve = @event.ScrewCurve;
        var title = $"{@event.ProductCode} | {curve.ScrewId} | {@event.Cave}";

        var plotModel = new PlotModel()
        {
            Title = title,
        };
        var legend = new Legend()
        {
            LegendPosition = LegendPosition.RightTop,
            LegendPlacement = LegendPlacement.Inside,
            LegendBorder = OxyColors.LightGray,
            LegendBorderThickness = 2,
            ShowInvisibleSeries = true,


        };
        plotModel.Legends.Add(legend);



        //var uslSeries = new LineSeries()
        //{
        //    ItemsSource = curve.TorqueList.Select((_, i) => new DataPoint(curve.AngleList[i], usl)),
        //    Title = $"USL({usl})",
        //};
        //plotModel.Series.Add(uslSeries);

        //var lslSeries = new LineSeries()
        //{
        //    ItemsSource = curve.TorqueList.Select((_, i) => new DataPoint(curve.AngleList[i], lsl)),
        //    Title = $"LSL({lsl})"
        //};
        //plotModel.Series.Add(lslSeries);

        var dataPoints = new List<DataPoint>();
        for (int i = 0; i < curve.AngleList.Count; i++)
        {
            dataPoints.Add(new DataPoint(i, curve.TorqueList[i]));
        }

        var dataSeries = new LineSeries
        {
            ItemsSource = dataPoints,
            InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline
        };
        plotModel.Series.Add(dataSeries);

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            IsPanEnabled = false,
            Title = "ANGLE(°)",

            // IntervalLength = pressureConfig.AxisXInterval,
        };
        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Title = $"TORQUE(in.ldf)",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            IsPanEnabled = false,
            IsZoomEnabled = true,
        };
        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);
        PlotHelper.ApplyTheme(plotModel);
        return plotModel;
    }
}

public class ScrewPlotModel
{
    public int PlotId { get; set; }
    public PlotModel PlotModel { get; set; }
}
