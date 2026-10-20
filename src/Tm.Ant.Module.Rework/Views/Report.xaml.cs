using System.Windows.Controls;

using Tm.Ant.Module.Rework.ViewModels;

namespace Tm.Ant.Module.Rework.Views;

public partial class Report : UserControl
{
    public Report(ReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
