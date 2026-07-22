using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.CustomeFieldServiceNS
{
    public interface ICustomFieldServiceClient : IDisposable
    {
        Task<(bool, object)> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
