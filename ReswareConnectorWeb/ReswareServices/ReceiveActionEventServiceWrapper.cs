using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveActionEventServiceWrapper : BaseRetryServiceWrapper<ReceiveActionEventServiceClient>, IReceiveActionEventServiceWrapper
    {
        public ReceiveActionEventServiceWrapper(
            ReceiveActionEventServiceClient client,
            ILogger<IntegrationService> logger)
            : base(client, logger)
        {
        }

        public async Task<ReceiveActionEventResponse> ReceiveActionEventAsync(ReceiveActionEventData data)
        {
            return await _client.ReceiveActionEventAsync(data);
        }
    }
}
