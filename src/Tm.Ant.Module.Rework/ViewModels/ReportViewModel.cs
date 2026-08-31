using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.ViewModels;
public partial class ReportViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectTimeIndex;
    [ObservableProperty]
    private string _selectDate=DateTime.Now.ToString();
    [ObservableProperty]
    private bool _isBusy;
    [ObservableProperty]
    private ObservableCollection<ReportVo> _dataList = new();
    public RelayCommand<string> QueryData { get; set; }

    private readonly IRepositoryFactory _repositoryFactory;
    public ReportViewModel(IRepositoryFactory repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
        QueryData = new RelayCommand<string>(async p=>await OnQueryDataAsync(p));
    }

    private async Task OnQueryDataAsync(string productCode)
    {
        try
        {
            DataList.Clear();
            IsBusy = true;

            var time = GetTimeRange(DateTime.Parse(SelectDate));
            using var repo = _repositoryFactory.CreateRepository<FlowMainHis>();
            var list = await repo.GetListAsync(t => t.CreateTime > time.Item1 && t.CreateTime < time.Item2);
            if (!string.IsNullOrEmpty(productCode)&& list?.Any()==true)
            {
                list=list.FindAll(x=>x.ProductCode== productCode);
            }
            if (list != null)
            {
                foreach(var item in list)
                {
                    DataList.Add(new ReportVo() 
                    {
                        Id= item.Id,
                        ProductCode = productCode,
                        UnScrewCaves=item.UnScrewCaves,
                        ResultJson=item.ResultJson,
                        Locations=item.Locations,
                        UpdateTime=item.UpdateTime,
                        CreateTime=item.CreateTime,
                        FinalResult=item.GetFinalResult(),
                    });
                }
            }
        }
        catch
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    private (DateTime, DateTime) GetTimeRange(DateTime dateTime)
    {
        var date = dateTime.Date;
        if (SelectTimeIndex == 0)
        {
            return (date.AddHours(8), date.AddDays(1).AddHours(8));
        }
        else if (SelectTimeIndex == 1)
        {
            return (date.AddHours(8), date.AddHours(20));
        }
        else if (SelectTimeIndex == 2)
        {
            return (date.AddHours(20), date.AddDays(1).AddHours(8));
        }
        return (date.AddHours(8), date.AddDays(1).AddHours(8));
    }
}
public class ReportVo: FlowMainHis
{
    public string FinalResult { get; set; }
}
