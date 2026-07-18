using ReswareConnectorWeb.CustomeFieldServiceNS;
using ReswareConnectorWeb.Models;
using ReswareConnectorWeb.RetryServices;

namespace ReswareConnectorWeb.ReswareServices
{
    public class CustomFieldServiceWrapper : BaseRestRetryServiceWrapper<ICustomFieldServiceClient>, ICustomFieldServiceWrapper
    {
        public CustomFieldServiceWrapper(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService,
            ILogger logger)
            : base(() => clientFactory.CreateCustomFieldServiceClient(), retryPolicyService, logger)
        {
        }

        public async Task<bool> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields)
        {
            return await ExecuteWithRetryAsync(
                () => _client.UpdateCustomFieldsAsync(fileId, customFields),
                channel => channel != null);
        }
    }
}
