using System.Text.Json;

namespace ReswareConnectorWeb.Extensions
{
    public static class StringExtensions
    {
        public static string Limit(this string input, int maxLength, bool addEllipsis = false)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (maxLength <= 0)
                return string.Empty;

            if (input.Length <= maxLength)
                return input;

            if (addEllipsis && maxLength > 3)
            {
                return input.Substring(0, maxLength - 3) + "...";
            }

            return input.Substring(0, maxLength);
        }
    }
}
