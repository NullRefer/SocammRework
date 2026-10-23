namespace Tm.Ant.Module.Rework.Models;

/// <summary>
/// 本机 SFC 接口参数，使用框架配置管理持久化。
/// </summary>
[DictJsonConfig("SfcConfigOption", "SFC 配置管理", "上料确认、PASS过站参数")]
public sealed class SfcConfigModel : IConfigNormalizer<SfcConfigModel>, IConfigValidator
{
    public string Emp { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string RecordUrl { get; set; } = "";
    public string CheckUrl { get; set; } = "";
    public string Group { get; set; } = "";

    public SfcConfigModel Normalize()
    {
        Emp = Emp?.Trim() ?? "";
        MachineCode = MachineCode?.Trim() ?? "";
        RecordUrl = RecordUrl?.Trim() ?? "";
        CheckUrl = CheckUrl?.Trim() ?? "";
        Group = Group?.Trim() ?? "";
        return this;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var url in new[] { RecordUrl, CheckUrl }.Where(value => value.Length > 0))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                errors.Add($"SFC接口地址无效：{url}");
            }
        }

        return errors;
    }
}
