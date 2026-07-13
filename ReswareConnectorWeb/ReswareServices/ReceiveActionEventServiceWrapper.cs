using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveActionEventServiceWrapper : BaseRetryServiceWrapper<ReceiveActionEventServiceClient>, IReceiveActionEventServiceWrapper
    {
        public ReceiveActionEventServiceWrapper(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService)
            : base(() => clientFactory.CreateReceiveActionEventServiceClient(), retryPolicyService)
        {
        }

        public async Task<ReceiveActionEventResponse> ReceiveActionEventAsync(ReceiveActionEventData data)
        {
            return await ExecuteWithRetryAsync(
                () => _client.ReceiveActionEventAsync(data),
                channel => channel != null && channel.State != CommunicationState.Faulted);
        }
    }
}
