using ActionEventServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface IReceiveActionEventServiceWrapper : IDisposable
    {
        Task<ReceiveActionEventResponse> ReceiveActionEventAsync(ReceiveActionEventData data);
    }
}
