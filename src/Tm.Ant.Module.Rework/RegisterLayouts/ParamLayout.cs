using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;

internal sealed class ReworkParamLayout(IServiceProvider services) : ParamLayout(services)
{
    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        RegisterLeftRightLayout<SfcConfig>("重工线SFC");
    }
}
