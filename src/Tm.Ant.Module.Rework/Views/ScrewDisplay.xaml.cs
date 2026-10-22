using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class ScrewDisplay : UserControl
{
    public ScrewDisplay(ScrewDisplayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
