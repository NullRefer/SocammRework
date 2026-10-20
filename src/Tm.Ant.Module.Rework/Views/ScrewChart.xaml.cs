using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class ScrewChart : UserControl
{
    public ScrewChart(ScrewChartViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
