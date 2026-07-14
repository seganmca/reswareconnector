namespace ReswareConnectorWeb.Config
{
    public class ServiceClientOptions
    {
        public ServiceConfiguration ReceiveNoteService { get; set; } = new();
        public ServiceConfiguration ReceiveActionEventService { get; set; } = new();
        public ServiceConfiguration ReceiveSearchDataService { get; set; } = new();
        public ServiceConfiguration CustomFieldService { get; set; } = new();
    }
}
