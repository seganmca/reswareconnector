using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface ICustomFieldServiceWrapper : IDisposable
    {
        Task<(bool, object)> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
