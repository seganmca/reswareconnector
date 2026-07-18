using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Services;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace ReswareConnectorWeb.ReswareServices
{
    public abstract class BaseServiceClientFactory
    {
        protected readonly ServiceClientOptions _options;
        protected readonly ILogger<IntegrationService> _logger;

        protected BaseServiceClientFactory(ServiceClientOptions options, ILogger<IntegrationService> logger)
        {
            _options = options;
            _logger = logger;
        }

        protected virtual CustomBinding CreateBinding()
        {
            _logger.LogInformation("CreateBinding");
            // 1. Bootstrap: UserName over HTTPS
            var bootstrap = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
            bootstrap.IncludeTimestamp = true;
            bootstrap.MessageSecurityVersion =
                MessageSecurityVersion
                    .WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
            bootstrap.LocalClientSettings.DetectReplays = false;

            // 2. SecureConversation wrapper
            var security = SecurityBindingElement.CreateSecureConversationBindingElement(bootstrap);
            security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Default;
            security.IncludeTimestamp = false;
            security.MessageSecurityVersion = bootstrap.MessageSecurityVersion;
            security.LocalClientSettings.DetectReplays = false;

            // 3. Binary message encoding
            var encoding = new BinaryMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.Soap12WSAddressing10
            };

            // 4. HTTPS transport
            var transport = new HttpsTransportBindingElement
            {
                RequireClientCertificate = false
            };

            // 5. Final binding
            return new CustomBinding(security, encoding, transport);
        }

        protected virtual EndpointAddress CreateEndpointAddress(string serviceUrl)
        {
            return new EndpointAddress(serviceUrl);
        }

        protected virtual void ConfigureClientCredentials(System.ServiceModel.Description.ClientCredentials credentials, string username, string password)
        {
            var certificate = credentials?.ClientCertificate?.Certificate;
            if (certificate != null)
            {
                _logger.LogInformation("ConfigureClientCredentials. FriendlyName : {FriendlyName}, Version : {Version}, IssuerName : {IssuerName}, MatchesHostname : {MatchesHostname}", certificate.FriendlyName, certificate.Version, certificate.IssuerName, certificate.MatchesHostname);
            }
            credentials.UserName.UserName = username;
            credentials.UserName.Password = password;
            _logger.LogInformation("ConfigureClientCredentials. UserName : {UserName}, Password : {Password}", username, password);
        }
    }
}
