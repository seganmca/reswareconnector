using ReswareConnectorWeb.CustomeFieldServiceNS;
using ReswareConnectorWeb.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ReswareConnectorWeb.Connected_Services.CustomeFieldServiceNS
{
    public class CustomFieldServiceClient : ICustomFieldServiceClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        public CustomFieldServiceClient(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl.TrimEnd('/');

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // Only for development
            };

            _httpClient = new HttpClient(handler);

            // Set Basic Authentication
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        }

        public async Task<bool> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields)
        {
            var url = $"{_baseUrl}/files/{fileId}/customfields";

            var xmlPayload = SerializeToXml(customFields);
            var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return true;
        }

        private string SerializeToXml(FileCustomFields customFields)
        {
            var serializer = new XmlSerializer(typeof(FileCustomFields));

            // Create namespaces to match the exact format
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("i", "http://www.w3.org/2001/XMLSchema-instance");
            // Don't add a default namespace here - it's handled by the XmlRoot attribute

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            serializer.Serialize(xmlWriter, customFields, namespaces);
            return stringWriter.ToString();
        }


        private FileCustomFields? DeserializeFromXml(string xml)
        {
            var serializer = new XmlSerializer(typeof(FileCustomFields));
            using var stringReader = new StringReader(xml);
            return serializer.Deserialize(stringReader) as FileCustomFields;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
