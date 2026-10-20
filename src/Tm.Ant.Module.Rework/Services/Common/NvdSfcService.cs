using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Provider;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class NvdSfcService(IHttpService http, IConfigService configs, IShieldProvider shield) : INvdSfcService
{
    private static readonly string[] Positions = ["172", "170", "168", "212", "174", "176", "178", "180"];

    public async Task<NvdReworkResponse> GetCavesAsync(string code, CancellationToken token = default)
    {
        var switches = await shield.GetShieldAsync(token);
        if (!switches.Sfc)
        {
            return new NvdReworkResponse { DoCaves = FlowDictKeys.TestCaves.ToArray() };
        }

        var config = await configs.GetAsync<SfcConfigModel>(token);
        if (string.IsNullOrWhiteSpace(config.ReworkUrl))
        {
            return new NvdReworkResponse { Rc = 9999, Result = "SFC需拆查询地址未配置。" };
        }

        var response = await http.PostJsonAsync(
            config.ReworkUrl, new NvdReworkRequest { Sn = code }, LogCategory.SFC, token);
        if (!response.IsSuccess)
        {
            token.ThrowIfCancellationRequested();
            return new NvdReworkResponse { Rc = response.Code == 0 ? 9999 : response.Code, Result = response.Message };
        }

        NvdReworkResponse data;
        try
        {
            data = JsonConvert.DeserializeObject<NvdReworkResponse>(response.Data);
        }
        catch (JsonException ex)
        {
            return new NvdReworkResponse { Rc = 9999, Result = $"SFC需拆响应解析失败：{ex.Message}" };
        }

        if (data == null)
        {
            return new NvdReworkResponse { Rc = 9999, Result = "SFC需拆响应为空。" };
        }

        if (!data.IsSuccess)
        {
            return data;
        }

        var groups = config.Group.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!groups.Contains(data.Group))
        {
            return new NvdReworkResponse { Rc = 9999, Result = $"产品({code})站点({data.Group})不属于配置站点({config.Group})。" };
        }

        var locations = (data.Locations ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lga = locations.Where(value => value.StartsWith('L')).Select(value => value[1..]).Order().ToArray();
        var socamm = locations.Where(value => value.StartsWith('J')).Select(value => value[1..]).Order().ToArray();
        if (lga.Length == 0 || !lga.SequenceEqual(socamm) || lga.Any(value => !Positions.Contains(value)))
        {
            return new NvdReworkResponse { Rc = 9999, Result = $"产品({code})需拆位置({data.Locations})无效：要求已知且成对的L/J位置。" };
        }

        data.DoCaves = new short[8];
        foreach (var location in lga)
        {
            data.DoCaves[Array.IndexOf(Positions, location)] = 1;
        }

        return data;
    }

    public async Task<PassResponse> StationRecordAsync(RecoredRequest request, CancellationToken token = default)
    {
        var switches = await shield.GetShieldAsync(token);
        if (!switches.Sfc)
        {
            return new PassResponse();
        }

        var config = await configs.GetAsync<SfcConfigModel>(token);
        if (string.IsNullOrWhiteSpace(config.RecordUrl))
        {
            return new PassResponse { Rc = 9999, Result = "SFC过站接口地址未配置。" };
        }

        request.Emp = config.Emp;
        request.MachineCode = config.MachineCode;
        var response = await http.PostJsonAsync(config.RecordUrl, request, LogCategory.SFC, token);
        if (!response.IsSuccess)
        {
            token.ThrowIfCancellationRequested();
            return new PassResponse { Rc = response.Code == 0 ? 9999 : response.Code, Result = response.Message };
        }

        try
        {
            return JsonConvert.DeserializeObject<PassResponse>(response.Data)
                ?? new PassResponse { Rc = 9999, Result = "SFC过站响应为空。" };
        }
        catch (JsonException ex)
        {
            return new PassResponse { Rc = 9999, Result = $"SFC过站响应解析失败：{ex.Message}" };
        }
    }
}
