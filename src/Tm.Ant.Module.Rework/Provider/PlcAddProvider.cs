using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.Provider;
internal interface IPlcAddProvider : ISingleton
{
    ScanAddress GetAddress(FlowType flowType);
}
internal class PlcAddProvider : IPlcAddProvider
{
    public ScanAddress GetAddress(FlowType flowType)
    {
        if (flowType == FlowType.LD)
        {
            return GetLdDb();
        }
        else if (flowType == FlowType.ULD)
        {
            return GetUldDb();
        }
        return GetUnscrewDb();
    }
    private UnscrewAddress GetUnscrewDb()
    {
        return new UnscrewAddress()
        {
            ScanTrigger = "D100",
            ScanResult = "D200",
            ProductAdd = "D201",
            UnscrewFlag = "D232",
            UnscrewLocationDb = "D233",
            Workplace1 = new UnscrewWp1()
            {
                ProductDb = "D101",
                DoCaveDb = "D132",
                FirstScrewPGNum = "D133",
                ThirdScrewPGNum = "D134",
                FirstScrewNo="D169",
                ThirdScrewNo="D170"
            },
            Workplace2 = new UnscrewWp2()
            {
                ProductDb = "D135",
                DoCaveDb = "D166",
                SecondScrewPGNum = "D167",
                SecondScrewNo="D171",
                UnscrewResult = "D168",
            },
        };
    }
    private ScanAddress GetLdDb()
    {
        return new ScanAddress()
        {
            ScanTrigger = "D100",
            ScanResult = "D200",
            ProductAdd = "D201",
        };
    }
    private ScanAddress GetUldDb()
    {
        return new ScanAddress()
        {
            ScanTrigger = "D101",
            ScanResult = "D201",
            ProductAdd = "D232",
        };
    }
}
