using System.Text.Json.Serialization;
using System.Text.Json;
using ReceiveNoteServiceNS;

namespace ReswareConnectorWeb.Converters
{
    public class ReceiveCurativeTypeEnumConverter : JsonConverter<ReceiveCurativeTypeEnum>
    {
        private static readonly Dictionary<string, ReceiveCurativeTypeEnum> _mappings =
            new Dictionary<string, ReceiveCurativeTypeEnum>(StringComparer.OrdinalIgnoreCase)
        {
        // Exact matches
        { "PRE_CLOSING", ReceiveCurativeTypeEnum.PRE_CLOSING },
        { "POLICY", ReceiveCurativeTypeEnum.POLICY },
        
        // Numeric strings
        { "0", ReceiveCurativeTypeEnum.PRE_CLOSING },
        { "1", ReceiveCurativeTypeEnum.POLICY }
        };

        public override ReceiveCurativeTypeEnum Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString();

                // Try our custom mappings first
                if (_mappings.TryGetValue(stringValue, out var enumValue))
                {
                    return enumValue;
                }

                // Try standard enum parsing
                if (Enum.TryParse<ReceiveCurativeTypeEnum>(stringValue, true, out enumValue))
                {
                    return enumValue;
                }

                // If still not found, try to normalize
                var normalized = stringValue.ToUpperInvariant()
                    .Replace(" ", "_")
                    .Replace("-", "_");

                if (_mappings.TryGetValue(normalized, out enumValue) ||
                    Enum.TryParse<ReceiveCurativeTypeEnum>(normalized, true, out enumValue))
                {
                    return enumValue;
                }

                throw new JsonException($"Invalid ReceiveCurativeTypeEnum value: '{stringValue}'");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                int intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(ReceiveCurativeTypeEnum), intValue))
                {
                    return (ReceiveCurativeTypeEnum)intValue;
                }
                throw new JsonException($"Invalid ReceiveCurativeTypeEnum numeric value: {intValue}");
            }

            throw new JsonException($"Unexpected token type for ReceiveCurativeTypeEnum: {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            ReceiveCurativeTypeEnum value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
