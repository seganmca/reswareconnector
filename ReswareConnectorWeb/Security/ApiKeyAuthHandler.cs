using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Services;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ReswareConnectorWeb.Security
{
    public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AuthenticationConfig.ApiKeyConfig config;
        private readonly string? apiKey;

        public ApiKeyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            AuthenticationConfig config) : base(options, logger, encoder)
        {
            this.config = config.ApiKey;
            apiKey = Utilities.GetEnvironmentVariableAnywhere(this.config.EnvironmentVariable);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"API Key environment variable '{this.config.EnvironmentVariable}' not configured");
            }
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(config.HeaderName, out var apiKeyHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (apiKeyHeader != apiKey)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "API Key User"),
                new Claim(ClaimTypes.AuthenticationMethod, "ApiKey")
            }, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
