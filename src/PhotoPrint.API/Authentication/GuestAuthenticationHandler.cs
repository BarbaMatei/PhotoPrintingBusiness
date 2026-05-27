using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Authentication;

public class GuestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "GuestToken";
    public const string HeaderName = "X-Guest-Token";
    public const string GuestSessionIdClaimType = "guest_session_id";

    private readonly IServiceScopeFactory _scopeFactory;

    public GuestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var tokenHeader))
        {
            return AuthenticateResult.NoResult();
        }

        if (!Guid.TryParse(tokenHeader.ToString(), out var sessionId))
        {
            return AuthenticateResult.Fail("Format invalid pentru X-Guest-Token.");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var session = await db.GuestSessions.FindAsync(sessionId);

        if (session is null || !session.IsValid)
        {
            return AuthenticateResult.Fail("Sesiunea de oaspete a expirat sau este invalidă.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, sessionId.ToString()),
            new Claim(GuestSessionIdClaimType, sessionId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
