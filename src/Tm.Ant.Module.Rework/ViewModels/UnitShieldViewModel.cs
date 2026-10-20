using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.ViewModels;

public partial class UnitShieldViewModel : ObservableObject
{
    private readonly IShieldProvider _shields;
    private readonly IDialogService _dialogs;
    private readonly Task _loading;
    [ObservableProperty] private bool _sfcEnabled;
    public ObservableCollection<TestCaveItem> TestCaves { get; } = new();

    public UnitShieldViewModel(IShieldProvider shields, IDialogService dialogs)
    {
        _shields = shields;
        _dialogs = dialogs;
        for (var i = 1; i <= 8; i++)
        {
            TestCaves.Add(new TestCaveItem(i));
        }

        _loading = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            SfcEnabled = (await _shields.GetShieldAsync()).Sfc;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"屏蔽配置读取失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleSfcAsync()
    {
        await _loading;
        try
        {
            var value = await _shields.GetShieldAsync();
            var draft = new ShieldModel { Sfc = !value.Sfc, IsFakeMode = value.IsFakeMode };
            await _shields.SaveAsync(draft);
            SfcEnabled = draft.Sfc;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"屏蔽配置保存失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private void ApplyTestCaves()
    {
        FlowDictKeys.TestCaves = TestCaves.Select(item => (short)(item.Selected ? 1 : 0)).ToArray();
        _dialogs.ShowInfo("模拟需拆穴位已应用，仅SFC屏蔽时使用。");
    }
}

public partial class TestCaveItem(int number) : ObservableObject
{
    public int Number { get; } = number;
    [ObservableProperty] private bool _selected;
}
