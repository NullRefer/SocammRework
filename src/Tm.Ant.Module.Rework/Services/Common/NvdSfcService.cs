using Tm.Ant.Module.Rework.Interfaces.Common;
using Tm.Ant.Module.Rework.Models.SFC;

namespace Tm.Ant.Module.Rework.Services.Common;

internal sealed class NvdSfcService : INvdSfcService
{
    private readonly IHttpService _httpService;
    private readonly IConfigService _configService;
    private static readonly string[] Positions = ["172", "170", "168", "212", "174", "176", "178", "180"];

    public NvdSfcService(IHttpService httpService, IConfigService configService)
    {
        _httpService = httpService;
        _configService = configService;
    }

    public async Task<OperationResult<(short[] Caves, string Locations)>> GetCavesAsync(string code,
        CancellationToken token = default)
    {
        var switches = await _configService.GetAsync<ShieldModel>(token);
        if (!switches.Sfc)
        {
            return OperationResult.Success<(short[], string)>((FlowDictKeys.TestCaves.ToArray(), null));
        }

        var config = await _configService.GetAsync<SfcConfigModel>(token);
        if (string.IsNullOrWhiteSpace(config.CheckUrl))
        {
            return OperationResult.Error<(short[], string)>("SFC需拆查询地址未配置。", 9999);
        }

        var response = await _httpService.PostJsonAsync(
            config.CheckUrl, new CheckRequest { Sn = code }, LogCategory.SFC, token);
        if (!response.IsSuccess)
        {
            token.ThrowIfCancellationRequested();
            return OperationResult.Error<(short[], string)>($"SFC需拆查询请求失败：{response.Message}",
                response.Code == 0 ? 9999 : response.Code);
        }

        CheckResponse data;
        try
        {
            data = JsonConvert.DeserializeObject<CheckResponse>(response.Data);
        }
        catch (JsonException ex)
        {
            return OperationResult.Error<(short[], string)>($"SFC需拆响应解析失败：{ex.Message}", 9999);
        }

        if (data == null)
        {
            return OperationResult.Error<(short[], string)>("SFC需拆响应为空。", 9999);
        }

        if (data.Rc != 0)
        {
            return OperationResult.Error<(short[], string)>($"SFC上料确认失败，产品({code})：{data.Result}", data.Rc);
        }

        var groups = config.Group.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!groups.Contains(data.Group))
        {
            return OperationResult.Error<(short[], string)>(
                $"产品({code})站点({data.Group})不属于配置站点({config.Group})。", 9999);
        }

        var locations = (data.Locations ?? "").Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lga = locations.Where(value => value.StartsWith('L')).Select(value => value[1..]).Order().ToArray();
        var socamm = locations.Where(value => value.StartsWith('J')).Select(value => value[1..]).Order().ToArray();
        if (lga.Length == 0 || !lga.SequenceEqual(socamm) || lga.Any(value => !Positions.Contains(value)))
        {
            return OperationResult.Error<(short[], string)>(
                $"产品({code})需拆位置({data.Locations})无效：要求已知且成对的L/J位置。", 9999);
        }

        var caves = new short[8];
        foreach (var location in lga)
        {
            caves[Array.IndexOf(Positions, location)] = 1;
        }

        return OperationResult.Success((caves, data.Locations));
    }

    public async Task<OperationResult> StationRecordAsync(string code, CancellationToken token = default)
    {
        var switches = await _configService.GetAsync<ShieldModel>(token);
        if (!switches.Sfc)
        {
            return OperationResult.Success("SFC过站已屏蔽，模拟成功。");
        }

        var config = await _configService.GetAsync<SfcConfigModel>(token);
        if (string.IsNullOrWhiteSpace(config.RecordUrl))
        {
            return OperationResult.Error("SFC过站接口地址未配置。", 9999);
        }

        var request = new StationRecordRequest
        {
            Sn = code,
            Result = "PASS",
            ErrorCode = "N/A",
            Emp = config.Emp,
            MachineCode = config.MachineCode
        };
        var response = await _httpService.PostJsonAsync(config.RecordUrl, request, LogCategory.SFC, token);
        if (!response.IsSuccess)
        {
            token.ThrowIfCancellationRequested();
            return OperationResult.Error($"SFC过站请求失败：{response.Message}",
                response.Code == 0 ? 9999 : response.Code);
        }

        StationRecordResponse data;
        try
        {
            data = JsonConvert.DeserializeObject<StationRecordResponse>(response.Data);
        }
        catch (JsonException ex)
        {
            return OperationResult.Error($"SFC过站响应解析失败：{ex.Message}", 9999);
        }

        if (data == null)
        {
            return OperationResult.Error("SFC过站响应为空。", 9999);
        }

        if (data.Rc != 0)
        {
            return OperationResult.Error($"SFC过站确认失败，产品({code})：{data.Result}", data.Rc);
        }

        return OperationResult.Success(data.Result ?? "SFC过站成功。");
    }
}
