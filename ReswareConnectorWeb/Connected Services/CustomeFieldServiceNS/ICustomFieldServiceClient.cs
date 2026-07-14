using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.CustomeFieldServiceNS
{
    public interface ICustomFieldServiceClient : IDisposable
    {
        Task<bool> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
