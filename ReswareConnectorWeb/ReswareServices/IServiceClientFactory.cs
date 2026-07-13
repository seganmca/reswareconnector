using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface IServiceClientFactory
    {
        ReceiveNoteServiceClient CreateReceiveNoteServiceClient();
        ReceiveActionEventServiceClient CreateReceiveActionEventServiceClient();
        ReceiveSearchDataServiceClient CreateReceiveSearchDataServiceClient();
    }
}
