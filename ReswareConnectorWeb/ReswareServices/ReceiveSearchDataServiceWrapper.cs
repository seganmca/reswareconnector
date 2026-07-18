using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using SearchDataServiceNS;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveSearchDataServiceWrapper : BaseRetryServiceWrapper<ReceiveSearchDataServiceClient>, IReceiveSearchDataServiceWrapper, IDisposable
    {
        public ReceiveSearchDataServiceWrapper(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService)
            : base(()=> clientFactory.CreateReceiveSearchDataServiceClient(), retryPolicyService)
        {
        }

        public async Task<ReceiveSearchDataResponse> ReceiveSearchDataAsync(ReceiveSearchDataData SearchData)
        {
            return await ExecuteWithRetryAsync(
                          () => _client.ReceiveSearchDataAsync(SearchData),
                          channel => channel != null && channel.State != CommunicationState.Faulted);
        }
    }
}
