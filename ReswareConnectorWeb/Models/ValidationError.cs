namespace ReswareConnectorWeb.Models
{
    public class ValidationError
    {
        public string Property { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string? AttemptedValue { get; set; }
    }
}
