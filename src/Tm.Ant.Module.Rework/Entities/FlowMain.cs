namespace Tm.Ant.Module.Rework.Entities;

/// <summary>
/// 一次上料创建的生产记录，结果和下线记录均保留在主表。
/// </summary>
[Table("flow")]
public sealed class FlowMain : EntityBase
{
    [Column("productCode")]
    public string ProductCode { get; set; } = "";

    /// <summary>
    /// 按穴位1至8排列的0/1标记，例如1,0,1,0,0,0,0,0。
    /// </summary>
    [Column("unScrewCaves")]
    public string UnScrewCaves { get; set; } = "";

    [Column("locations")]
    public string Locations { get; set; } = "";

    [Column("resultJson")]
    public string ResultJson { get; set; } = "[]";

    public short[] GetRequiredCaves()
    {
        var parts = (UnScrewCaves ?? "").Split(',');
        if (parts.Length != 8 || parts.Any(value => value is not ("0" or "1")))
        {
            throw new FormatException($"产品({ProductCode})需拆标记无效：{UnScrewCaves}，要求8个0/1。");
        }

        return parts.Select(short.Parse).ToArray();
    }

    public List<ResultJsonItem> GetResults()
    {
        return string.IsNullOrWhiteSpace(ResultJson)
            ? new List<ResultJsonItem>()
            : JsonConvert.DeserializeObject<List<ResultJsonItem>>(ResultJson) ?? new List<ResultJsonItem>();
    }

    public Dictionary<int, string> GetFinalResult()
    {
        var required = GetRequiredCaves();
        var received = GetResults();
        var results = new Dictionary<int, string>();
        for (var index = 0; index < required.Length; index++)
        {
            if (required[index] != 1)
            {
                continue;
            }

            var result = received.FirstOrDefault(item => item.Cave == index + 1);
            results[index + 1] = result?.Result == "OK" ? "OK" : "NG";
        }

        return results;
    }
}

/// <summary>
/// PLC上传的单个穴位最终拆除结果，不含螺丝批回调数据。
/// </summary>
public sealed class ResultJsonItem
{
    public int Cave { get; set; }
    public string Result { get; set; } = "";
}
