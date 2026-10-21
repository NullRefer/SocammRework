using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class ManualPassViewModel : ObservableObject
{
    private readonly INvdSfcService _nvdSfcService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ManualPassViewModel> _logger;

    public ManualPassViewModel(INvdSfcService nvdSfcService, IDialogService dialogService,
        ILogger<ManualPassViewModel> logger)
    {
        _nvdSfcService = nvdSfcService;
        _dialogService = dialogService;
        _logger = logger;
    }

    [ObservableProperty]
    public partial string ProductCode { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; }

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
            var result = await _nvdSfcService.StationRecordAsync(ProductCode.Trim());
            Message = result.ToJson();

            if (!result.IsSuccess)
            {
                _dialogService.ShowError($"手动过站失败：{result.Message}");
                return;
            }

            _dialogService.ShowSuccess($"手动补 {ProductCode} 结果: {Message}");
            _logger.LogInformation($"手动操作补 {ProductCode} 节点, 补节点结果: {Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while manual add sfc for {Code}", ProductCode);
            _dialogService.ShowError($"补节点异常, 异常信息: {ex}");
        }
    }
}
