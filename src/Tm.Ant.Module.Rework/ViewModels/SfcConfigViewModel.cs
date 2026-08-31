using System.Windows;

using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.FlowServices;
using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.ViewModels;
internal partial class SfcConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private SfcConfigModel _sfc;
    [ObservableProperty]
    private string _productCode;

    public RelayCommand<string> ExecuteCmd { get; set; }
    public RelayCommand<string> TestSfcCmd { get; set; }

    private readonly IConfiguration _configuration;
    private readonly INvdSfcService _nvdSfcService;
    public SfcConfigViewModel(IConfiguration configuration,INvdSfcService nvdSfcService)
    {
        _configuration= configuration;
        _nvdSfcService= nvdSfcService;
        ExecuteCmd = new RelayCommand<string>(OnExecuteCmd);
        TestSfcCmd = new RelayCommand<string>(OnTestSfcCmd);
        var config = _configuration.GetReworkOption();
        Sfc = config.Sfc;
    }

    private void OnExecuteCmd(string flag)
    {
        if (flag == "reset")
        {
            IsEnabled = true;
        }
        else if (flag == "save")
        {
            _configuration.SaveReworkOption(x =>
            {
                x.Sfc = Sfc;
            });
            IsEnabled= false;
        }
    }
    private async void OnTestSfcCmd(string flag)
    {
        if (flag == "Rework")
        {
            var resp= await _nvdSfcService.GetCavesAsync(ProductCode);
            MessageBox.Show(JsonHelper.SerializeObject(resp));
        }
        else if (flag == "Record")
        {
           var resp= await _nvdSfcService.StationRecordAsync(new RecoredRequest() 
            {
                  Sn=ProductCode,
                  Result="PASS",
            });
            MessageBox.Show(JsonHelper.SerializeObject(resp));
        }
    }
}
