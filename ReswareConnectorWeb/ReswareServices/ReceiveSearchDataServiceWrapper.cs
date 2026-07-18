using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;
using SearchDataServiceNS;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveSearchDataServiceWrapper : BaseRetryServiceWrapper<ReceiveSearchDataServiceClient>, IReceiveSearchDataServiceWrapper, IDisposable
    {
        public ReceiveSearchDataServiceWrapper(
            ReceiveSearchDataServiceClient client,
            ILogger<IntegrationService> logger)
            : base(client, logger)
        {
        }

        public async Task<ReceiveSearchDataResponse> ReceiveSearchDataAsync(ReceiveSearchDataData SearchData)
        {
            return await _client.ReceiveSearchDataAsync(SearchData);
        }
    }
}
