using Org.BouncyCastle.Asn1.Ocsp;
using Serilog.Filters;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.Serialization;

namespace ReswareConnectorWeb.Services
{
    public static class IOHelper
    {
        public static async Task<byte[]> ReadAllBytesAsync(string file)
        {
            if (File.Exists(file))
            {
                return await File.ReadAllBytesAsync(file);
            }
            throw new Exception($"File '{file}' Not Found");
        }

        public static async Task CreateJsonFileAsync<T>(T data, string file)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };

            var jsonString = JsonSerializer.Serialize(data, jsonOptions);
            await File.WriteAllTextAsync(file, jsonString);
        }

        public static async Task CreateXmlFileAsync<T>(T data, string file)
        {
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));

                var dataType = data.GetType();
                var xmlSerializer = new XmlSerializer(dataType);

                var xmlSettings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = Encoding.UTF8,
                    Async = true
                };

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                {
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", "");
                    xmlSerializer.Serialize(xmlWriter, data, namespaces);
                    await File.WriteAllTextAsync(file, stringWriter.ToString(), Encoding.UTF8);
                }
            }
            catch(Exception ex)
            {

            }
        }

        public static async Task<T> LoadJsonFileAsync<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"JSON file not found: {filePath}");
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = await File.ReadAllTextAsync(filePath);
                var result = JsonSerializer.Deserialize<T>(jsonString, jsonOptions);

                if (result is null)
                {
                    throw new InvalidOperationException($"Failed to deserialize JSON file: {filePath}");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading JSON file {filePath}: {ex.Message}", ex);
            }
        }
       

        public static void CreateDirectory(string? path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static List<string> GetDirectories(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return new List<string>();

            var directoryInfo = new DirectoryInfo(path);

            IEnumerable<string> matchedDirectories = directoryInfo.EnumerateDirectories().OrderBy(f => f.LastWriteTime).Select(f => f.Name);

            return matchedDirectories.Distinct().ToList();
        }
    }
}
