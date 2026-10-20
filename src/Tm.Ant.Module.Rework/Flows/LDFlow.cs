using Tm.Ant.Module.Rework.Interfaces.Flows;
using Tm.Ant.Module.Rework.Models.Pto;

namespace Tm.Ant.Module.Rework.Flows;

/// <summary>
/// Load扫码交互入口，清零由框架Runtime在结果反馈之后执行。
/// </summary>
internal sealed class LDFlow(IFlowRuntimeService runtime, ILoadService service) : IFlowBase
{
    private readonly LoadAddress _address = new();
    public string FlowName => FlowType.LD.ToString();

    public Task RunAsync(CancellationToken token)
    {
        if (!runtime.CheckDevices(DeviceIds.MainPlc, DeviceIds.ProductScanner))
        {
            return Task.CompletedTask;
        }

        return runtime.CreatePollingTask(t => runtime.RunOnTriggerAsync(DeviceIds.MainPlc,
            "上料扫码建档", _address.Trigger,
            ct => service.ScanAsync(_address, DeviceIds.ProductScanner, ct), t), FlowName, token);
    }
}
