using Tm.Ant.Framework.Models.Events;

namespace Tm.Ant.Module.Rework.Models;
internal class FlowEventPubSub : PubSubEvent<FlowEventPubSub>
{
    public string ProductCode { get; set; }
    public Dictionary<int, string> CaveResults { get; set; } = new();
}
