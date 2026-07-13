using SearchDataServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface IReceiveSearchDataServiceWrapper : IDisposable
    {

        Task<ReceiveSearchDataResponse> ReceiveSearchDataAsync(ReceiveSearchDataData SearchData);
    }
}
