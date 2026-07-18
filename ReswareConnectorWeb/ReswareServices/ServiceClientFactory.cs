using ActionEventServiceNS;
using Microsoft.Extensions.Options;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Connected_Services.CustomeFieldServiceNS;
using ReswareConnectorWeb.CustomeFieldServiceNS;
using ReswareConnectorWeb.Helpers;
using ReswareConnectorWeb.Services;
using SearchDataServiceNS;
using System.Net;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ServiceClientFactory : BaseServiceClientFactory, IServiceClientFactory
    {
        private readonly ILogger<IntegrationService> _logger;
        public ServiceClientFactory(IOptions<ServiceClientOptions> options, ILogger<IntegrationService> logger)
                    : base(options.Value, logger) 
        {
            _logger = logger;
        }

        public ReceiveNoteServiceClient CreateReceiveNoteServiceClient()
        {
            _logger.LogInformation("CreateReceiveNoteServiceClient called");
            var config = _options.ReceiveNoteService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveNoteServiceClient(binding, endpointAddress);
            var (username, password) = GetUserNamePassword(config);
            _logger.LogInformation("CreateReceiveNoteServiceClient called : {Username}, {Password}", username, password);
            //ConfigureClientCredentials(client.ClientCredentials, username, password);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;

            return client;
        }

        public ReceiveActionEventServiceClient CreateReceiveActionEventServiceClient()
        {
            _logger.LogInformation("CreateReceiveActionEventServiceClient called");
            var config = _options.ReceiveActionEventService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveActionEventServiceClient(binding, endpointAddress);
            var behavior = new SoapLoggerBehavior(_logger);
            client.Endpoint.EndpointBehaviors.Add(behavior);

            var (username, password) = GetUserNamePassword(config);
            _logger.LogInformation("CreateReceiveActionEventServiceClient called : {Username}, {Password}", username, password);
            //ConfigureClientCredentials(client.ClientCredentials, username, password);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;

            return client;
        }

        public ReceiveSearchDataServiceClient CreateReceiveSearchDataServiceClient()
        {
            _logger.LogInformation("CreateReceiveSearchDataServiceClient called");
            var config = _options.ReceiveSearchDataService;
            var binding = CreateBinding();
            var endpointAddress = CreateEndpointAddress(config.ServiceUrl);

            var client = new ReceiveSearchDataServiceClient(binding, endpointAddress);
            var (username, password) = GetUserNamePassword(config);
            _logger.LogInformation("CreateReceiveSearchDataServiceClient called : {Username}, {Password}", username, password);
            //ConfigureClientCredentials(client.ClientCredentials, username, password);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;

            return client;
        }

        public ICustomFieldServiceClient CreateCustomFieldServiceClient()
        {
            _logger.LogInformation("CreateCustomFieldServiceClient called");
            var config = _options.CustomFieldService;
            var (username, password) = GetUserNamePassword(config);

            _logger.LogInformation("CreateCustomFieldServiceClient called : {Username}, {Password}", username, password);
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
