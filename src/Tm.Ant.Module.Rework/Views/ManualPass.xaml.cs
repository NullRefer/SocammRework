using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class ManualPass : UserControl
{
    public ManualPass(ManualPassViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
