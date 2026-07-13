using System.Text.Json;

namespace ReswareConnectorWeb.Extensions
{

    public static class JsonElementExtensions
    {
        public static bool TryGetPropertyCaseInsensitive(
            this JsonElement element,
            string propertyName,
            out JsonElement value,
            JsonSerializerOptions? options = null)
        {
            value = default;

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            // Use custom case comparison
            var comparison = options?.PropertyNameCaseInsensitive == true
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, comparison))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        public static JsonElement GetPropertyCaseInsensitive(
            this JsonElement element,
            string propertyName,
            JsonSerializerOptions? options = null)
        {
            if (TryGetPropertyCaseInsensitive(element, propertyName, out var value, options))
                return value;

            throw new KeyNotFoundException($"Property '{propertyName}' not found in JSON object.");
        }

        public static bool HasPropertyCaseInsensitive(
            this JsonElement element,
            string propertyName,
            JsonSerializerOptions? options = null)
        {
            return TryGetPropertyCaseInsensitive(element, propertyName, out _, options);
        }

        public static Dictionary<string, JsonElement> ToDictionaryCaseInsensitive(
            this JsonElement element,
            JsonSerializerOptions? options = null)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, JsonElement>();

            var comparison = options?.PropertyNameCaseInsensitive == true
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var dict = new Dictionary<string, JsonElement>(StringComparer.FromComparison(comparison));

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value;
            }

            return dict;
        }
    }

}
