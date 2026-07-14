using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface ICustomFieldServiceWrapper : IDisposable
    {
        Task<bool> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
