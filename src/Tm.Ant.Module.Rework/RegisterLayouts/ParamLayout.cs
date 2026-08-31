using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;
internal sealed class ReworkParamLayout : ParamLayout
{
    public ReworkParamLayout(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        RegisterLeftRightLayout<CommConfig>(CommonViewWpf.LangResources.Menu_Param_CommPage);
        RegisterLeftRightLayout<SfcConfig>("SFC");
    }
}
