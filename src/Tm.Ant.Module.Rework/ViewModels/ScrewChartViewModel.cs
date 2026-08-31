using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.ViewModels;
 
public partial class ScrewChartViewModel : ScrewCurveBase, INavigationAware
{
    [ObservableProperty]
    private string _selectDate;
    public RelayCommand<string> QueryCurveCmd { get; set; }

    public readonly IScrewCurveProvider _screwCurveProvider;
    public ScrewChartViewModel(IScrewCurveProvider screwCurveProvider, IEventBus eventBus, IConfiguration configuration) : base(configuration)
    {
        eventBus.Subscribe<ScrewCurveEvent>(OnScrewCurveEvent);
        _screwCurveProvider = screwCurveProvider;
        QueryCurveCmd = new RelayCommand<string>(OnQueryCurve);
    }

    private void OnQueryCurve(string productCode)
    {
        if (string.IsNullOrEmpty(SelectDate))
        {
            return;
        }
        else if (string.IsNullOrEmpty(productCode))
        {
            return;
        }
        var date = DateTime.Parse(SelectDate).ToString("yyyy-MM-dd");
    }

    public void OnNavigatedFrom()
    {

    }

    public void OnNavigatedTo(object parameter)
    {
        RefreshPlots(_screwCurveProvider.GetCurveFromMemory());
    }

    private void OnScrewCurveEvent(ScrewCurveEvent @event)
    {
        RefreshPlots(_screwCurveProvider.GetCurveFromMemory());
    }
}
