using System.Windows.Controls;
using System.Windows.Media; 

using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Views;
/// <summary>
/// UnitShield.xaml 的交互逻辑
/// </summary>
public partial class UnitShield : UserControl
{
    private readonly IShieldProvider _shiledProvider;
    public UnitShield(IShieldProvider shiledProvider)
    {
        _shiledProvider = shiledProvider;
        InitializeComponent();
        Loaded += UnitShield_Loaded;
        InitTestCave();
    }

    private async void UnitShield_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var shield = await _shiledProvider.GetShieldAsync();
            SetShieldColor(shield);
        }
        catch { }
    }

    private async void btnSfc_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if(btnSfc.Tag == null) return;
        var shield=btnSfc.Tag as ShieldModel;
        shield.Sfc = !shield.Sfc;
        SetShieldColor(shield);
        await _shiledProvider.SaveAsync(shield); 
    }
    private async void btnFake_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (btnFake.Tag == null) return;
        var shield = btnFake.Tag as ShieldModel;
        shield.IsFakeMode = !shield.IsFakeMode;
        if (shield.IsFakeMode)
        {
            shield.Sfc = false;
        }
        SetShieldColor(shield);
        await _shiledProvider.SaveAsync(shield);
    }

    private void SetShieldColor(ShieldModel shield)
    {
        btnSfc.Tag = shield;
        btnFake.Tag = shield;
        if (!shield.Sfc)
        {
            btnSfc.Background = Brushes.Red;
        }
        else
        {
            btnSfc.Background = Brushes.Green;
        }

        if (!shield.IsFakeMode)
        {
            btnFake.Background = Brushes.Red;
        }
        else
        {
            btnFake.Background = Brushes.Green;
        }
        testCaveGroup.Visibility = shield.Sfc ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    private void InitTestCave()
    {
        foreach(var ui in wpCave.Children)
        {
            if(ui is CheckBox cb)
            {
                cb.Click += (s, e) => 
                {
                    FlowDictKeys.TestCaves = ParseCaves();
                };
            }
        }
    }
    private short[] ParseCaves()
    {
        var caves =new short[8];
        foreach (var ui in wpCave.Children)
        {
            if (ui is CheckBox cb)
            {
                int icave = int.Parse(cb.Content.ToString());
                if (cb.IsChecked==true)
                {
                    caves[icave - 1] = 1;
                }
                else
                {
                    caves[icave - 1] = 0;
                }
            }
        }
        return caves;
    }
}
