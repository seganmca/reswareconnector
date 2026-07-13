namespace ReswareConnectorWeb.Config
{
    public class AuthenticationConfig
    {
        public ApiKeyConfig ApiKey { get; set; } = new();

        public class ApiKeyConfig
        {
            public string HeaderName { get; set; } = "X-API-KEY";
            public string EnvironmentVariable { get; set; } = string.Empty;
        }
    }
}
