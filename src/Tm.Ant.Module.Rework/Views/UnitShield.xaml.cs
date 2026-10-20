using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class UnitShield : UserControl
{
    public UnitShield(UnitShieldViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
