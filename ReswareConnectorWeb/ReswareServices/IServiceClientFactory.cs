using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.CustomeFieldServiceNS;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface IServiceClientFactory
    {
        ReceiveNoteServiceClient CreateReceiveNoteServiceClient();
        ReceiveActionEventServiceClient CreateReceiveActionEventServiceClient();
        ReceiveSearchDataServiceClient CreateReceiveSearchDataServiceClient();
        ICustomFieldServiceClient CreateCustomFieldServiceClient();
    }
}
