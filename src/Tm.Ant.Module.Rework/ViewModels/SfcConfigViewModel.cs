using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.SFC;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class SfcConfigViewModel : ObservableObject, INavigationAware
{
    private readonly IConfigService _configs;
    private readonly INvdSfcService _sfcService;
    private readonly IDialogService _dialogs;
    private readonly ILogger _logger;
    private Task _loading = Task.CompletedTask;
    private CancellationTokenSource _session;

    public SfcConfigViewModel(IConfigService configs, INvdSfcService sfcService, IDialogService dialogs,
        ILogger<SfcConfigViewModel> logger)
    {
        _configs = configs;
        _sfcService = sfcService;
        _dialogs = dialogs;
        _logger = logger;
    }

    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private SfcConfigModel _sfc = new();
    [ObservableProperty] private string _productCode = "";
    [ObservableProperty] private string _message = "";

    [RelayCommand]
    private async Task ExecuteAsync(string action)
    {
        try
        {
            var token = _session?.Token ?? CancellationToken.None;
            if (action == "reset")
            {
                var config = await _configs.GetAsync<SfcConfigModel>(token);
                if (!token.IsCancellationRequested)
                {
                    Sfc = JsonConvert.DeserializeObject<SfcConfigModel>(JsonConvert.SerializeObject(config));
                }
                return;
            }

            Sfc = await _configs.SaveAsync(Sfc, token);
            _dialogs.ShowSuccess("SFC配置保存完成。");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SFC配置读取/保存失败");
            _dialogs.ShowError($"SFC配置操作失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestSfcAsync(string action)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductCode))
            {
                _dialogs.ShowWarning("请输入产品码。");
                return;
            }

            if (action == "Record" && !await _dialogs.ShowConfirmAsync($"确认向SFC上传产品({ProductCode})的PASS过站？"))
            {
                return;
            }

            var token = _session?.Token ?? CancellationToken.None;
            object result = action == "Rework"
                ? await _sfcService.GetCavesAsync(ProductCode.Trim(), token)
                : await _sfcService.StationRecordAsync(new StationRecordRequest
                {
                    Sn = ProductCode.Trim(),
                    Result = "PASS",
                    ErrorCode = "N/A"
                }, token);
            Message = JsonConvert.SerializeObject(result, Formatting.Indented);
            _dialogs.ShowInfo(Message);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SFC接口测试失败");
            _dialogs.ShowError($"SFC接口测试失败：{ex.Message}");
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        _session?.Cancel();
        _session?.Dispose();
        _session = new CancellationTokenSource();
        _loading = ExecuteAsync("reset");
    }

    public void OnNavigatedFrom()
    {
        _session?.Cancel();
    }
}
