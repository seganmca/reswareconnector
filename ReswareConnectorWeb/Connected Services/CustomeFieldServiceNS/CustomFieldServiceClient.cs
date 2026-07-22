using ReswareConnectorWeb.CustomeFieldServiceNS;
using ReswareConnectorWeb.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using YamlDotNet.Core.Tokens;

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
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true, // Only for development
                AllowAutoRedirect = false
            };

            _httpClient = new HttpClient(handler);

            // Set Basic Authentication
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        }

        public async Task<(bool, object)> UpdateCustomFieldsAsync(long fileId, FileCustomFields customFields)
        {
            var url = $"{_baseUrl}/files/{fileId}/customfields";

            var jsonPayload = SerializeToJson(customFields);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(responseData);

            return (true, json);
        }

        private string SerializeToJson(FileCustomFields customFields)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(customFields, options);
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
