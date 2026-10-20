using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class PCBResultDisplay : UserControl
{
    public PCBResultDisplay(PCBResultDisplayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
