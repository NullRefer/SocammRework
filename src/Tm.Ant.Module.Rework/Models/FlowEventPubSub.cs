using Tm.Ant.Framework.Models.Events;

namespace Tm.Ant.Module.Rework.Models;

public class FlowEventPubSub : PubSubEvent<FlowEventPubSub>
{
    public bool IsOk { get; set; }
    public string ProductCode { get; set; }
    public Dictionary<int, string> CaveResults { get; set; } = new();
}
