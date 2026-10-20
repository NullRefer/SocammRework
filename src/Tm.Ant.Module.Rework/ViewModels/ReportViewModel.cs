using Tm.Ant.CommonViewWpf.Models;
using Tm.Ant.Module.Rework.Entities;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class ReportViewModel : ObservableObject, INavigationAware
{
    private readonly IProductQueryService _query;
    private readonly IDialogService _dialogs;
    private readonly ILogger _logger;
    private CancellationTokenSource _session;
    private Task _loading = Task.CompletedTask;

    public ReportViewModel(IProductQueryService query, IDialogService dialogs, ILogger<ReportViewModel> logger)
    {
        _query = query;
        _dialogs = dialogs;
        _logger = logger;
        Pagination.OnPageChanged = _ => LoadAsync();
    }

    [ObservableProperty] private DateTime _selectDate = DateTime.Today;
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _productCode = "";
    [ObservableProperty] private bool _isBusy;
    public PaginationVo<FlowMain> Pagination { get; } = new() { PageSize = 50 };

    [RelayCommand]
    private async Task QueryDataAsync()
    {
        if (IsBusy)
        {
            return;
        }

        Pagination.PageIndex = 1;
        _loading = LoadAsync();
        await _loading;
    }

    private async Task LoadAsync()
    {
        if (IsBusy || _session == null || _session.IsCancellationRequested)
        {
            return;
        }

        if (EndDate.Date < SelectDate.Date)
        {
            _dialogs.ShowWarning("结束日期不能早于开始日期。");
            return;
        }

        var token = _session.Token;
        IsBusy = true;
        try
        {
            var result = await _query.QueryAsync(SelectDate.Date, EndDate.Date.AddDays(1), ProductCode,
                Pagination.PageIndex, Pagination.PageSize, token);
            if (!token.IsCancellationRequested)
            {
                Pagination.UpdateFromPagedResultDto(result);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "产品报表查询失败");
            _dialogs.ShowError($"产品报表查询失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        _session?.Cancel();
        _session?.Dispose();
        _session = new CancellationTokenSource();
        var previous = _loading;
        _loading = ReloadAfterAsync(previous, _session.Token);
    }

    private async Task ReloadAfterAsync(Task previous, CancellationToken token)
    {
        await previous;
        if (!token.IsCancellationRequested)
        {
            await LoadAsync();
        }
    }

    public void OnNavigatedFrom()
    {
        _session?.Cancel();
    }
}
