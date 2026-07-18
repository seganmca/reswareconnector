using ActionEventServiceNS;
using Microsoft.Extensions.Options;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Connected_Services.CustomeFieldServiceNS;
using ReswareConnectorWeb.CustomeFieldServiceNS;
using ReswareConnectorWeb.Services;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ServiceClientFactory : BaseServiceClientFactory, IServiceClientFactory
    {
        public ServiceClientFactory(IOptions<ServiceClientOptions> options)
                    : base(options.Value) 
        {
        }

        public ReceiveNoteServiceClient CreateReceiveNoteServiceClient()
        {
            var config = _options.ReceiveNoteService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveNoteServiceClient(binding, endpointAddress);
            var (username, password) = GetUserNamePassword(config);
            ConfigureClientCredentials(client.ClientCredentials, username, password);

            return client;
        }

        public ReceiveActionEventServiceClient CreateReceiveActionEventServiceClient()
        {
            var config = _options.ReceiveActionEventService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveActionEventServiceClient(binding, endpointAddress);
            var (username, password) = GetUserNamePassword(config);
            ConfigureClientCredentials(client.ClientCredentials, username, password);

            return client;
        }

        public ReceiveSearchDataServiceClient CreateReceiveSearchDataServiceClient()
        {
            var config = _options.ReceiveSearchDataService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveSearchDataServiceClient(binding, endpointAddress);
            var (username, password) = GetUserNamePassword(config);
            ConfigureClientCredentials(client.ClientCredentials, username, password);

            return client;
        }

        public ICustomFieldServiceClient CreateCustomFieldServiceClient()
        {
            var config = _options.CustomFieldService;
            var (username, password) = GetUserNamePassword(config);

            return new CustomFieldServiceClient(config.ServiceUrl, username, password);
        }

        private (string, string) GetUserNamePassword(ServiceConfiguration config)
        {
            var userName = Utilities.GetEnvironmentVariableAnywhere(config.UserNameVariable) ?? throw new ArgumentNullException($"{config.UserNameVariable} environment variable is required");
            var password = Utilities.GetEnvironmentVariableAnywhere(config.PasswordVariable) ?? throw new ArgumentNullException($"{config.PasswordVariable} environment variable is required");
            return (userName, password);
        }
    }
}
