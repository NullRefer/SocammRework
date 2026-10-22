using Tm.Ant.Framework.Models.Events;
using Tm.Ant.Module.Rework.Interfaces.Common;

namespace Tm.Ant.Module.Rework.ViewModels;

/// <summary>
/// 螺丝结果展示与24点标定，进入订阅、离开取消并丢弃未保存草稿。
/// </summary>
public partial class ScrewDisplayViewModel : ObservableObject, INavigationAware
{
    private readonly IConfigService _configService;
    private readonly IScrewDisplayService _screwDisplayService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly List<ScrewCalibrationPoint> _draft = [];
    private IReadOnlyList<ScrewCalibrationPoint> _saved = [];
    private ScrewDisplayChangedEvent _subscription;
    private CancellationTokenSource _session;
    private Task _saveTask = Task.CompletedTask;
    private bool _isActive;
    private int _navigationVersion;

    public ScrewDisplayViewModel(IConfigService configService, IScrewDisplayService screwDisplayService,
        IEventBus eventBus, IDialogService dialogService)
    {
        _configService = configService;
        _screwDisplayService = screwDisplayService;
        _eventBus = eventBus;
        _dialogService = dialogService;
    }

    [ObservableProperty]
    public partial string ProductCode { get; set; } = "等待下料结果";

    [ObservableProperty]
    public partial DateTime? TriggeredAt { get; set; }

    [ObservableProperty]
    public partial DateTime? UnloadTime { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "尚未标定，请先完成24点标定。";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial bool IsCalibrating { get; set; }

    [ObservableProperty]
    public partial bool SaveFailed { get; set; }

    public ObservableCollection<ScrewDisplayMarker> Markers { get; } = [];
    public Task NavigationLoadTask { get; private set; } = Task.CompletedTask;
    public bool CanStartCalibration => _isActive && !IsLoading && !IsSaving && !IsCalibrating;
    public bool CanCancelCalibration => _isActive && IsCalibrating && !IsSaving;
    public bool CanAddPoint => CanCancelCalibration && _draft.Count < ScrewDisplayCalibration.PositionCount;
    public bool CanRetrySave => CanCancelCalibration && SaveFailed
        && _draft.Count == ScrewDisplayCalibration.PositionCount;

    public void OnNavigatedTo(object parameter)
    {
        _isActive = true;
        var version = ++_navigationVersion;
        _session?.Cancel();
        _session?.Dispose();
        _session = new CancellationTokenSource();
        _subscription ??= _eventBus.Subscribe<ScrewDisplayChangedEvent>(OnDisplayChanged, ThreadOption.UIThread);
        IsLoading = true;
        RefreshResult();
        NavigationLoadTask = LoadCalibrationAsync(Task.WhenAll(NavigationLoadTask, _saveTask), version, _session.Token);
        UpdateCommands();
    }

    public void OnNavigatedFrom()
    {
        _isActive = false;
        _navigationVersion++;
        _session?.Cancel();
        _subscription?.Unsubscribe(OnDisplayChanged);
        _subscription = null;
        _draft.Clear();
        IsCalibrating = false;
        IsSaving = false;
        SaveFailed = false;
        UpdateCommands();
    }

    private async Task LoadCalibrationAsync(Task previous, int version, CancellationToken token)
    {
        try
        {
            await previous;
            token.ThrowIfCancellationRequested();
            var calibration = await _configService.GetAsync<ScrewDisplayCalibration>(token);
            if (!IsCurrent(version, token))
            {
                return;
            }

            _saved = calibration.Points.ToArray();
            Status = _saved.Count == 24 ? "标定已加载，等待或显示最新下料结果。" : "尚未标定，请先完成24点标定。";
            RefreshResult();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrent(version, token))
            {
                _saved = [];
                Markers.Clear();
                Status = "标定读取失败，可重新开始标定。";
                _dialogService.ShowError($"螺丝标定读取失败：{ex.Message}");
            }
        }
        finally
        {
            if (IsCurrent(version, token))
            {
                IsLoading = false;
                UpdateCommands();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartCalibration))]
    private void StartCalibration()
    {
        _draft.Clear();
        IsCalibrating = true;
        SaveFailed = false;
        UpdateCalibrationPreview();
    }

    [RelayCommand(CanExecute = nameof(CanCancelCalibration))]
    private void CancelCalibration()
    {
        _draft.Clear();
        IsCalibrating = false;
        SaveFailed = false;
        Status = _saved.Count == 24 ? "已取消标定，继续使用原标定。" : "尚未标定，请先完成24点标定。";
        RefreshResult();
        UpdateCommands();
    }

    [RelayCommand(CanExecute = nameof(CanAddPoint))]
    private async Task AddPointAsync(ScrewCalibrationPoint point)
    {
        if (!CanAddPoint || !ScrewDisplayGeometry.IsValid(point))
        {
            return;
        }

        _draft.Add(point);
        UpdateCalibrationPreview();
        if (_draft.Count == ScrewDisplayCalibration.PositionCount)
        {
            _saveTask = SaveCalibrationAsync(_navigationVersion, _session.Token);
            await _saveTask;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRetrySave))]
    private async Task RetrySaveAsync()
    {
        _saveTask = SaveCalibrationAsync(_navigationVersion, _session.Token);
        await _saveTask;
    }

    private async Task SaveCalibrationAsync(int version, CancellationToken token)
    {
        IsSaving = true;
        SaveFailed = false;
        Status = "24点已采集，正在保存标定。";
        UpdateCommands();
        var draft = new ScrewDisplayCalibration { Points = _draft.ToList() };
        try
        {
            var saved = await _configService.SaveAsync(draft, token);
            if (!IsCurrent(version, token))
            {
                return;
            }

            _saved = saved.Points.ToArray();
            _draft.Clear();
            IsCalibrating = false;
            Status = "24点标定已保存。";
            RefreshResult();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrent(version, token))
            {
                SaveFailed = true;
                Status = "标定保存失败，24点草稿已保留，请重试保存或取消。";
                _dialogService.ShowError($"螺丝标定保存失败：{ex.Message}");
            }
        }
        finally
        {
            if (IsCurrent(version, token))
            {
                IsSaving = false;
                UpdateCommands();
            }
        }
    }

    private void OnDisplayChanged(ScrewDisplayChangedEvent payload)
    {
        if (_isActive)
        {
            RefreshResult();
        }
    }

    private void RefreshResult()
    {
        var snapshot = _screwDisplayService.Latest;
        ProductCode = snapshot?.ProductCode ?? "等待下料结果";
        TriggeredAt = snapshot?.TriggeredAt;
        UnloadTime = snapshot?.UnloadTime;
        if (IsCalibrating)
        {
            return;
        }

        Markers.Clear();
        if (_saved.Count != ScrewDisplayCalibration.PositionCount || snapshot == null)
        {
            return;
        }

        foreach (var pair in snapshot.CaveResults.OrderBy(pair => pair.Key))
        {
            if (pair.Key is < 1 or > 8)
            {
                continue;
            }

            for (var screw = 0; screw < 3; screw++)
            {
                var index = (pair.Key - 1) * 3 + screw;
                Markers.Add(CreateMarker(index, _saved[index], pair.Value == "OK" ? "OK" : "NG"));
            }
        }
    }

    private void UpdateCalibrationPreview()
    {
        Markers.Clear();
        for (var index = 0; index < _draft.Count; index++)
        {
            Markers.Add(CreateMarker(index, _draft[index], ""));
        }

        var next = _draft.Count;
        Status = next == 24 ? "已采集24/24点。" : $"已采集{next}/24点，请点击穴位{next / 3 + 1}的第{next % 3 + 1}颗螺丝。";
        UpdateCommands();
    }

    private static ScrewDisplayMarker CreateMarker(int index, ScrewCalibrationPoint point, string result)
    {
        var label = string.IsNullOrEmpty(result) ? $"{index + 1}" : $"{index + 1} {result}";
        return new ScrewDisplayMarker(index, point.X * ScrewDisplayGeometry.ImageWidth,
            point.Y * ScrewDisplayGeometry.ImageHeight, result, label);
    }

    private bool IsCurrent(int version, CancellationToken token)
    {
        return _isActive && version == _navigationVersion && !token.IsCancellationRequested;
    }

    private void UpdateCommands()
    {
        StartCalibrationCommand.NotifyCanExecuteChanged();
        CancelCalibrationCommand.NotifyCanExecuteChanged();
        AddPointCommand.NotifyCanExecuteChanged();
        RetrySaveCommand.NotifyCanExecuteChanged();
    }
}
