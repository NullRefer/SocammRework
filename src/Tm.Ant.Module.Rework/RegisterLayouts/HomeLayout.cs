using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

public sealed class ReworkHomeLayout : HomeLayout
{
    public ReworkHomeLayout(IServiceProvider services)
        : base(services)
    {
    }

    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        RegisterLeftRightLayout<ManualPass>("手动过站");
        RegisterLeftRightLayout<Report>("产品报表");
    }
}
