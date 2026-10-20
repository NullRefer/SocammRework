namespace Tm.Ant.Module.Rework.Provider;

/// <summary>
/// 保存最近一次下料展示结果，导航返回时恢复界面。
/// </summary>
public interface IFlowResultState : ISingleton
{
    FlowEventPubSub Latest { get; }
    void Set(FlowEventPubSub result);
}

internal sealed class FlowResultState : IFlowResultState
{
    public FlowEventPubSub Latest { get; private set; }
    public void Set(FlowEventPubSub result)
    {
        Latest = result;
    }
}
