using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.FlowServices;

public interface INvdSfcService : ISingleton
{
    Task<PassResponse> StationRecordAsync(RecoredRequest recoredRequest);
    Task<NvdReworkResponse> GetCavesAsync(string productCode);
}
internal class NvdSfcService : INvdSfcService
{
    private readonly static List<CaveLocation> CaveLocations = new()
    {
        new CaveLocation() { Lga = "L172", Socamm = "J172",Cave=1 },
        new CaveLocation() { Lga = "L170", Socamm = "J170" ,Cave=2},
        new CaveLocation() { Lga = "L168", Socamm = "J168" ,Cave=3},
        new CaveLocation() { Lga = "L212", Socamm = "J212" ,Cave=4},
        new CaveLocation() { Lga = "L174", Socamm = "J174" ,Cave=5},
        new CaveLocation() { Lga = "L176", Socamm = "J176" ,Cave=6},
        new CaveLocation() { Lga = "L178", Socamm = "J178" ,Cave=7},
        new CaveLocation() { Lga = "L180", Socamm = "J180" ,Cave=8},
    };

    private readonly IShieldProvider _shiledProvider;
    private readonly IConfiguration _configuration;
    private readonly IHttpService _httpService; 
    public NvdSfcService(IHttpService httpService, IConfiguration configuration, IShieldProvider shiledProvider)
    {
        _httpService = httpService;
        _configuration= configuration;
        _shiledProvider= shiledProvider;
    }

    public async Task<NvdReworkResponse> GetCavesAsync(string productCode)
    {
        var (sfcEnabled, config) = await GetConfigAsync();
        if (!sfcEnabled)
        {
            return new NvdReworkResponse()
            {
                DoCaves = FlowDictKeys.TestCaves,
            };
        }
        var url = config.ReworkUrl;
        //query from SFC system
        var resp = await _httpService.PostAsync<NvdReworkResponse>(url, new NvdReworkRequest() {Sn=productCode, }, LogCategory.SFC);
        if (resp == null)
        {
            return new NvdReworkResponse() 
            {
                  Rc=9999,
                  Result="Invoke service fail!",
            };
        }

        if (!config.Group.Split(',').Contains(resp.Group))
        {
            return new NvdReworkResponse
            {
                Rc = resp.Rc,
                Result = $"{productCode} 非当前站点作业, SFC 反馈应在: {resp.Group}, 本线体仅作业: {config.Group}, 请联系生产确认来料",
                Group = resp.Group
            };
        }

        var locList = resp.Locations?.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(c => c.StartsWith("L"));

        if (locList == null)
        {
            return resp;
        }

        var allLocations = resp.Locations?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var loList = allLocations.Where(c => c.StartsWith("L")).ToList();
        var joList = allLocations.Where(c => c.StartsWith("J")).ToList();

        if (loList.Count == 0 || loList.Count != joList.Count)
        {
            return new NvdReworkResponse
            {
                Rc = resp.Rc,
                Result = $"{productCode} 物料对应的 soc与lga 数量不匹配, 即 L,J 数量未对应, 请联系生产确认来料",
                Group = resp.Group
            };
        }

        var lNumbers = loList.Select(l => l.Substring(1)).OrderBy(n => n).ToList();
        var jNumbers = joList.Select(j => j.Substring(1)).OrderBy(n => n).ToList();

        if (!lNumbers.SequenceEqual(jNumbers))
        {
            return new NvdReworkResponse
            {
                Rc = resp.Rc,
                Result = $"{productCode} 物料对应的 soc与lga 位置不匹配, 即 L,J 位置未对应, 请联系生产确认来料",
                Group = resp.Group
            };
        }

        //mapping
      
        if (locList != null)
        {
            foreach (var l in locList)
            {
                var caveLocation = CaveLocations.Find(c => c.Lga == l);
                if (caveLocation != null)
                {
                    resp.DoCaves[caveLocation.Cave - 1] = 1;
                }
            }
        }
        return resp;
    }

    public async Task<PassResponse> StationRecordAsync(RecoredRequest recoredRequest)
    {
        var (sfcEnabled, config) = await GetConfigAsync();
        if (!sfcEnabled)
        {
            return new PassResponse();
        }
        var url = config.RecordUrl;
        recoredRequest.Emp = config.Emp;
        recoredRequest.MachineCode = config.MachineCode;
        var resp= await _httpService.PostAsync<PassResponse>(url, recoredRequest, LogCategory.SFC);
        if (resp == null)
        {
            return new PassResponse()
            {
                Rc = 9999,
                Result = "网络异常"
            };
        }
        return  resp;
    }

    private async Task<(bool sfcEnabled, SfcConfigModel config)> GetConfigAsync()
    {
        var shield = await _shiledProvider.GetShieldAsync();
        var config = _configuration.GetReworkOption();
        return (shield.Sfc, config.Sfc);
    }
}

internal class CaveLocation
{
    public short Cave { get; set; }
    public string Lga { get; set; }
    public string Socamm { get; set; }


}
