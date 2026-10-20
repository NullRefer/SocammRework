namespace Tm.Ant.Module.Rework.Entities;
[Table("flow")]
public class FlowMain : EntityBase
{
    [Column("productCode")]
    public string ProductCode { get; set; }
    /// <summary>
    /// 需要拆除的穴位列表多个用英文逗号分隔，例如: 1,2,3
    /// </summary>
    [Column("unScrewCaves")]
    public string UnScrewCaves { get; set; }
    [Column("locations")]
    public string Locations { get; set; }
    /// <summary>
    /// 拆螺丝的结果json
    /// </summary>
    [Column("resultJson")]
    public string ResultJson { get; set; } 

    public Dictionary<int,string> GetFinalResult()
    {
        var unscrewCaveList = new List<int>();
        var oringCaves = UnScrewCaves.Split(',');
        for (int i = 0; i < oringCaves.Length; i++)
        {
            if (oringCaves[i] == "1")
            {
                unscrewCaveList.Add(i + 1);
            }
        }

        var unscrewResults = JsonHelper.DeserializeObject<List<ResultJsonItem>>(ResultJson);
        var results = new Dictionary<int, string>();
        foreach (var unscrewCave in unscrewCaveList)
        {
            var unscrewResult = unscrewResults.Find(r => r.Cave == unscrewCave);
            if (unscrewResult == null || unscrewResult.Result != "OK")
            {
                results.Add(unscrewCave, "NG");
            }
            else
            {
                results.Add(unscrewCave, "OK");
            }
        }

        return results;
    }
}

public class ResultJsonItem
{
    public int Cave { get; set; }
    public string Result { get; set; }
}
