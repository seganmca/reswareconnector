using Microsoft.Extensions.Logging;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ReswareConnectorWeb.Services
{
    public class SoapLogger : IClientMessageInspector
    {
        private readonly ILogger<IntegrationService> _logger;

        public SoapLogger(ILogger<IntegrationService> logger)
        {
            _logger = logger;
        }

        public object? BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var buffer = request.CreateBufferedCopy(int.MaxValue);

            request = buffer.CreateMessage();
            var copy = buffer.CreateMessage();

            _logger.LogInformation(
                "========== SOAP REQUEST ==========\n{SoapRequest}",
                MessageToString(copy));

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object? correlationState)
        {
            var buffer = reply.CreateBufferedCopy(int.MaxValue);

            reply = buffer.CreateMessage();
            var copy = buffer.CreateMessage();

            _logger.LogInformation(
                "========== SOAP RESPONSE ==========\n{SoapResponse}",
                MessageToString(copy));
        }

        private static string MessageToString(Message message)
        {
            using var sw = new StringWriter();
            using var xw = System.Xml.XmlWriter.Create(sw, new System.Xml.XmlWriterSettings
            {
                Indent = true
            });

            message.WriteMessage(xw);
            xw.Flush();

            return sw.ToString();
        }
    }
}
