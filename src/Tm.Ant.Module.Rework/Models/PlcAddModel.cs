namespace Tm.Ant.Module.Rework.Models;
internal class ScanAddress
{
    public string ScanTrigger { get; set; }
    public string ScanResult { get; set; }
    public string ProductAdd { get; set; }
}
internal class UnscrewAddress : ScanAddress
{
    /// <summary>
    /// 1-更新，PLC清零
    /// </summary>
    public string UnscrewFlag { get; set; }
    /// <summary>
    /// 拆螺丝定位数组
    /// </summary>
    public string UnscrewLocationDb { get; set; }
    public UnscrewWp1 Workplace1 { get; set; } = new();
    public UnscrewWp2 Workplace2 { get; set; } = new();
}
internal class UnscrewWp1
{
    public string ProductDb { get; set; }
    /// <summary>
    /// 当前加工哪个穴
    /// </summary>
    public string DoCaveDb { get; set; }
    /// <summary>
    /// 1 号螺丝拆除使用的程序号
    /// </summary>
    public string FirstScrewPGNum { get; set; }
    /// <summary>
    /// 1 号螺丝使用哪把枪 拆
    /// </summary>
    public string FirstScrewNo { get; set; }

    /// <summary>
    /// 3 号螺丝拆除使用的程序号
    /// </summary>
    public string ThirdScrewPGNum { get; set; }
    /// <summary>
    /// 3 号螺丝使用哪把枪 拆
    /// </summary>
    public string ThirdScrewNo { get; set; }
}
internal class UnscrewWp2
{
    public string ProductDb { get; set; }
    /// <summary>
    /// 当前加工哪个穴
    /// </summary>
    public string DoCaveDb { get; set; }
    /// <summary>
    /// 2 号螺丝拆除使用的程序号
    /// </summary>
    public string SecondScrewPGNum { get; set; }
    /// <summary>
    /// 2  号螺丝使用哪把枪 拆
    /// </summary>
    public string SecondScrewNo { get; set; }
    /// <summary>
    /// 当前产品拆螺丝最终结果1-ok;2-ng
    /// </summary>
    public string UnscrewResult { get; set; }
}

internal enum PlcResult
{
    /// <summary>
    /// 清零
    /// </summary>
    Zero = 0,
    /// <summary>
    /// OK
    /// </summary>
    OK = 1,
    /// <summary>
    /// NG2
    /// </summary>
    NG = 2,
}
