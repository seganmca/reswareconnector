using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveNoteServiceWrapper : BaseRetryServiceWrapper<ReceiveNoteServiceClient>, IReceiveNoteServiceWrapper
    {
        public ReceiveNoteServiceWrapper(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService)
            : base(() => clientFactory.CreateReceiveNoteServiceClient(), retryPolicyService)
        {
        }

        public async Task<ReceiveNoteResponse> ReceiveNoteAsync(ReceiveNoteData noteData)
        {
            return await ExecuteWithRetryAsync(
                () => _client.ReceiveNoteAsync(noteData),
                channel => channel != null && channel.State != CommunicationState.Faulted);
        }
    }
}
