using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.ViewModels;
public partial class CommConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private IpAndPort _scanner;

    [ObservableProperty]
    private IpAndPort _firstScrew;
    [ObservableProperty]
    private IpAndPort _secondScrew;

    public RelayCommand<string> ExecuteCmd { get; set; }
    public RelayCommand<IpAndPort> TestCmd { get; set; } 

    private readonly IConfiguration _configuration;
    public CommConfigViewModel(IConfiguration configuration)
    {
        _configuration = configuration;
        ExecuteCmd = new RelayCommand<string>(OnExecuteCmd);
        TestCmd = new RelayCommand<IpAndPort>(OnTestCmd);
        var config = _configuration.GetReworkOption();
        Scanner=config.Scanner; 
        FirstScrew =config.FirstScrew; 
        SecondScrew =config.SecondScrew; 
    }
     
    private void OnExecuteCmd(string flag)
    {
        if (flag == "reset")
        {
            IsEnabled = true;
        }
        else if(flag == "save")
        {
            _configuration.SaveReworkOption(x => 
            {
                x.Scanner = Scanner;
                x.FirstScrew = FirstScrew;
                x.SecondScrew= SecondScrew;
            });
            IsEnabled = false;
        }
    }
    private void OnTestCmd(IpAndPort sender)
    {

    }
}
