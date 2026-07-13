using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MySqlX.XDevAPI;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;

namespace ReswareConnectorWeb.Security
{
    public class WindowsAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public WindowsAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if Windows Identity is available
            var windowsIdentity = Context.User?.Identity as WindowsIdentity;

            if (windowsIdentity == null || !windowsIdentity.IsAuthenticated)
            {
                return AuthenticateResult.NoResult();
            }

            var identity = new ClaimsIdentity(windowsIdentity.Claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
    }
}
