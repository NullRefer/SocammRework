using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class SfcConfig : UserControl
{
    public SfcConfig(SfcConfigViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
