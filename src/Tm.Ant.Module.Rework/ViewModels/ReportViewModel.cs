using Tm.Ant.CommonViewWpf.Models;
using Tm.Ant.Module.Rework.Entities;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class ReportViewModel : ObservableObject, INavigationAware
{
    private readonly IProductQueryService _productQueryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger _logger;
    private CancellationTokenSource _session;
    private CancellationTokenSource _queryCancellation;
    private Task _loading = Task.CompletedTask;

    public ReportViewModel(IProductQueryService productQueryService, IDialogService dialogService,
        ILogger<ReportViewModel> logger)
    {
        _productQueryService = productQueryService;
        _dialogService = dialogService;
        _logger = logger;
        Pagination.OnPageChanged = _ => _loading = LoadAsync();
    }

    [ObservableProperty]
    public partial DateTime SelectDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime EndDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string ProductCode { get; set; } = "";

    public PaginationVo<FlowMain> Pagination { get; } = new() { PageSize = 50 };

    [RelayCommand]
    private async Task QueryDataAsync()
    {
        Pagination.PageIndex = 1;
        _loading = LoadAsync();
        await _loading;
    }

    private async Task LoadAsync()
    {
        if (_session == null || _session.IsCancellationRequested)
        {
            return;
        }

        if (EndDate.Date < SelectDate.Date)
        {
            _dialogService.ShowWarning("结束日期不能早于开始日期。");
            return;
        }

        _queryCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_session.Token);
        _queryCancellation = cancellation;
        var token = cancellation.Token;
        try
        {
            var request = new ProductQueryRequest(Pagination.PageIndex, Pagination.PageSize, ProductCode,
                SelectDate.Date, EndDate.Date.AddDays(1));
            var result = await _productQueryService.QueryAsync(request, token);
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
            if (token.IsCancellationRequested)
            {
                return;
            }

            _logger.LogError(ex, "产品报表查询失败");
            _dialogService.ShowError($"产品报表查询失败：{ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_queryCancellation, cancellation))
            {
                _queryCancellation = null;
            }
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
