using Tm.Ant.Module.Rework.Views;

namespace Tm.Ant.Module.Rework.RegisterLayouts;
internal class ReworkDataLayout: DataLayout
{
    public ReworkDataLayout(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    protected override void RegisterLayouts()
    {
        base.RegisterLayouts();
        //QueryResultPage
        RegisterLeftRightLayout<Report>("Query Result");
        RegisterLeftRightLayout<ScrewChart>("Screw Chart"); 
    }
}
