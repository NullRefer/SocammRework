using Tm.Ant.Module.Rework.Entities;

namespace Tm.Ant.Module.Rework.ViewModels;
public partial class ReportViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectTimeIndex;
    [ObservableProperty]
    private DateTime _selectDate=DateTime.Now.Date;
    [ObservableProperty]
    private bool _isBusy;
    [ObservableProperty]
    private ObservableCollection<FlowMain> _dataList = new();
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

            using var repo = _repositoryFactory.CreateRepository<FlowMain>();
            var list = (await repo.GetListAsync(t => t.CreateTime > SelectDate)).AsEnumerable();

            if (productCode.IsNotNullOrWhiteSpace())
            {
                list = list.Where(p => p.ProductCode.Contains(productCode));
            }
            if (list != null)
            {
                foreach(var item in list.OrderByDescending(e => e.Id))
                {
                    DataList.Add(item);
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
}
