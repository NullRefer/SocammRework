using Tm.Ant.Module.Rework.Extensions;
using Tm.Ant.Module.Rework.Models;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.FlowServices;

public interface INvdSfcService : ISingleton
{
    Task<NvdSfcResponse> StationRecordAsync(RecoredRequest recoredRequest);
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
        //mapping
        var locList = resp.Locations?.Split(',',StringSplitOptions.RemoveEmptyEntries).Where(c => c.StartsWith("L"));
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
        resp.PcbNg = resp.Group != config.Group;
        if (resp.PcbNg)
        {
            resp.DoCaves= new short[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        }
        return resp;
    }

    public async Task<NvdSfcResponse> StationRecordAsync(RecoredRequest recoredRequest)
    {
        var (sfcEnabled, config) = await GetConfigAsync();
        if (!sfcEnabled)
        {
            return new NvdSfcResponse();
        }
        var url = config.RecordUrl;
        recoredRequest.Emp = config.Emp;
        recoredRequest.MachineCode = config.MachineCode;
        var resp= await _httpService.PostAsync<NvdSfcResponse>(url, recoredRequest, LogCategory.SFC);
        if (resp == null)
        {
            return new NvdSfcResponse()
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
