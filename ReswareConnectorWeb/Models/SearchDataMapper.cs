using SearchDataServiceNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReswareConnectorWeb.Models
{
    public static class SearchDataMapper
    {
        public static ReceiveSearchDataData? MapToReceiveSearchDataData(ReceiveSearchDataDataDto? source)
        {
            if (source == null)
                return null;

            // Serialize the source DTO to JSON
            var json = JsonSerializer.Serialize(source, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            // Deserialize back to the target model
            // Any properties in the JSON that don't exist in the target model will be ignored
            return JsonSerializer.Deserialize<ReceiveSearchDataData>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
