using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ReceiveNoteServiceWrapper : BaseRetryServiceWrapper<ReceiveNoteServiceClient>, IReceiveNoteServiceWrapper
    {
        public ReceiveNoteServiceWrapper(
            ReceiveNoteServiceClient client,
            ILogger<IntegrationService> logger)
            : base(client, logger)
        {
        }

        public async Task<ReceiveNoteResponse> ReceiveNoteAsync(ReceiveNoteData noteData)
        {
            return await _client.ReceiveNoteAsync(noteData);
        }
    }
}
