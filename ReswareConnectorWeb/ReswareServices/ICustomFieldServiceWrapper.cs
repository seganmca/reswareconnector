using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface ICustomFieldServiceWrapper : IDisposable
    {
        Task<(bool, string)> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields);
    }
}
