namespace ReswareConnectorWeb.Config
{
    public class TitleHubConfig
    {
        public required string BaseUrl { get; set; }
        public required string ApiKeyName { get; set; }
        public required string ApiKeyValue { get; set; }

        public RetryPolicyConfig RetryPolicy { get; set; } = new();
    }
}
