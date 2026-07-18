using ReswareConnectorWeb.Config;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel;

namespace ReswareConnectorWeb.ReswareServices
{
    public abstract class BaseServiceClientFactory
    {
        protected readonly ServiceClientOptions _options;

        protected BaseServiceClientFactory(ServiceClientOptions options)
        {
            _options = options;
        }

        protected virtual CustomBinding CreateBinding()
        {
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
            credentials.UserName.UserName = username;
            credentials.UserName.Password = password;
        }
    }
}
