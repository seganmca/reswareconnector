using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReswareConnectorWeb.RawDataModels
{
    public class FlexibleOrderDto
    {
        public object? NoteData { get; set; }
        public object? SearchData { get; set; }
        public object? ActionEventData { get; set; }
        public bool SendNoteData { get; set; }
        public bool SendSearchData { get; set; }
        public bool SendActionEventData { get; set; }
    }

    // Helper class for JSON parsing
    [JsonConverter(typeof(FlexibleJsonConverter))]
    public class FlexibleJson
    {
        public JsonElement JsonElement { get; set; }
        public string? RawJson { get; set; }

        public FlexibleJson(JsonElement element)
        {
            JsonElement = element;
            RawJson = element.ToString();
        }

        public T? Deserialize<T>(JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(JsonElement, options);
            }
            catch (JsonException)
            {
                return default;
            }
        }
    }

    public class FlexibleJsonConverter : JsonConverter<FlexibleJson>
    {
        public override FlexibleJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new FlexibleJson(doc.RootElement.Clone());
        }

        public override void Write(Utf8JsonWriter writer, FlexibleJson value, JsonSerializerOptions options)
        {
            value.JsonElement.WriteTo(writer);
        }
    }

    public class FlexibleOrderDtoWithJson
    {
        public FlexibleJson? NoteData { get; set; }
        public FlexibleJson? SearchData { get; set; }
        public FlexibleJson? ActionEventData { get; set; }
        public bool SendNoteData { get; set; }
        public bool SendSearchData { get; set; }
        public bool SendActionEventData { get; set; }
    }
}
