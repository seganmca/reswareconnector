using System.Text.Json;

namespace ReswareConnectorWeb.Extensions
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static T FromJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }

        public static string ToJson<T>(this T obj)
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }
    }
}
