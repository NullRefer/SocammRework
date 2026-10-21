namespace Tm.Ant.Module.Rework.ViewModels;

public partial class UnitShieldViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IDialogService _dialogService;
    private readonly Task _loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectCaves))]
    [NotifyCanExecuteChangedFor(nameof(ApplyTestCavesCommand))]
    public partial bool SfcEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectCaves))]
    [NotifyCanExecuteChangedFor(nameof(ApplyTestCavesCommand))]
    public partial bool IsLoaded { get; set; }

    public bool CanSelectCaves => IsLoaded && !SfcEnabled;

    public ObservableCollection<TestCaveItem> TestCaves { get; } = new();

    public UnitShieldViewModel(IConfigService configService, IDialogService dialogService)
    {
        _configService = configService;
        _dialogService = dialogService;
        for (var i = 1; i <= 8; i++)
        {
            TestCaves.Add(new TestCaveItem(i) { Selected = FlowDictKeys.TestCaves[i - 1] == 1 });
        }

        _loading = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            SfcEnabled = (await _configService.GetAsync<ShieldModel>()).Sfc;
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"屏蔽配置读取失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleSfcAsync()
    {
        await _loading;
        try
        {
            var value = await _configService.GetAsync<ShieldModel>();
            var draft = new ShieldModel { Sfc = !value.Sfc };
            var saved = await _configService.SaveAsync(draft);
            SfcEnabled = saved.Sfc;
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"屏蔽配置保存失败：{ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectCaves))]
    private async Task ApplyTestCavesAsync()
    {
        try
        {
            SfcEnabled = (await _configService.GetAsync<ShieldModel>()).Sfc;
            if (SfcEnabled)
            {
                _dialogService.ShowWarning("请先关闭SFC，再应用需拆穴位。");
                return;
            }

            FlowDictKeys.TestCaves = TestCaves.Select(item => (short)(item.Selected ? 1 : 0)).ToArray();
            _dialogService.ShowInfo("需拆穴位已应用，仅SFC关闭时用于后续上料。");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"需拆穴位应用失败：{ex.Message}");
        }
    }
}

public partial class TestCaveItem : ObservableObject
{
    public TestCaveItem(int number)
    {
        Number = number;
    }

    public int Number { get; }

    [ObservableProperty]
    public partial bool Selected { get; set; }
}
