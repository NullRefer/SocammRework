using Microsoft.Win32;

using Tm.Ant.CommonViewWpf.Models;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.ViewModels;

/// <summary>
/// 管理产品报表的筛选、分页查询与导出。
/// </summary>
public partial class ReportViewModel : ObservableObject, INavigationAware
{
    private readonly IProductReportService _productReportService;
    private readonly IDialogService _dialogService;

    public ReportViewModel(IProductReportService productReportService, IDialogService dialogService)
    {
        _productReportService = productReportService;
        _dialogService = dialogService;
        Pagination.OnPageChanged = _ => QueryPageAsync();
    }

    [ObservableProperty]
    public partial DateTime? StartTime { get; set; } = DateTime.Today.AddHours(8);

    [ObservableProperty]
    public partial DateTime? EndTime { get; set; } = DateTime.Today.AddDays(1).AddHours(8);

    [ObservableProperty]
    public partial string ProductCode { get; set; } = "";

    public PaginationVo<ProductReportRowDto> Pagination { get; } = new() { PageSize = 20 };

    [RelayCommand]
    private async Task QueryDataAsync()
    {
        Pagination.PageIndex = 1;
        await QueryPageAsync();
    }

    private async Task QueryPageAsync()
    {
        try
        {
            Pagination.UpdateFromPagedResultDto(
                await _productReportService.QueryAsync(CreateRequest(), CancellationToken.None));
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel文件(*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"产品报表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var count = await _productReportService.ExportAsync(CreateRequest(), dialog.FileName, CancellationToken.None);
            _dialogService.ShowSuccess($"产品报表导出成功，共{count}条记录。");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    private ProductReportQueryRequest CreateRequest()
    {
        return new ProductReportQueryRequest(Pagination.PageIndex, Pagination.PageSize,
            ProductCode, StartTime.Value, EndTime.Value);
    }

    public void OnNavigatedTo(object parameter)
    {
        StartTime = DateTime.Today.AddHours(8);
        EndTime = DateTime.Today.AddDays(1).AddHours(8);
        ProductCode = "";
        Pagination.PageSize = 20;
        QueryDataCommand.Execute(null);
    }

    public void OnNavigatedFrom()
    {
    }
}
