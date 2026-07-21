using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.CustomeFieldServiceNS
{
    public interface ICustomFieldServiceClient : IDisposable
    {
        Task<(bool, string)> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
