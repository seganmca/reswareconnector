namespace ReswareConnectorWeb.ReswareServices
{
    public interface IServiceWrapperFactory
    {
        IReceiveNoteServiceWrapper CreateReceiveNoteService();
        IReceiveActionEventServiceWrapper CreateReceiveActionEventService();
        IReceiveSearchDataServiceWrapper CreateReceiveSearchDataService();
        ICustomFieldServiceWrapper CreateCustomFieldService();
    }
}
