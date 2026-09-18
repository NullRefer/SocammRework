using Tm.Ant.Module.Rework.FlowServices;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class ManualPassViewModel : ObservableObject
{
    private readonly INvdSfcService _sfcService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ManualPassViewModel> _logger;

    public ManualPassViewModel(INvdSfcService sfcService, IDialogService dialogService, ILogger<ManualPassViewModel> logger)
    {
        _sfcService = sfcService;
        _dialogService = dialogService;
        _logger = logger;
    }

    [ObservableProperty]
    private string _productCode;

    [ObservableProperty]
    private string _message;

    [RelayCommand]
    public async Task Pass()
    {
        Message = "";
        if (ProductCode.IsNullOrWhiteSpace())
        {
            _dialogService.ShowWarning("产品码不可为空");
            return;
        }

        if (!await _dialogService.ShowConfirmAsync($"是否确认手动补 {ProductCode} 节点?"))
        {
            return;
        }

        try
        {
            var result = await _sfcService.StationRecordAsync(new Models.RecoredRequest
            {
                Sn = ProductCode.Trim(),
                ErrorCode = "N/A",
                Result = "PASS"
            });
            Message = result.ToJson();

            _dialogService.ShowInfo($"手动补 {ProductCode} 结果: {Message}");
            _logger.LogInformation($"手动操作补 {ProductCode} 节点, 补节点结果: {Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while manual add sfc for {Code}", ProductCode);
            _dialogService.ShowError($"补节点异常, 异常信息: {ex}");
        }
    }
}
