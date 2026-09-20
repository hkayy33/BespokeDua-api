using System.Security.Claims;
using System.Text.Encodings.Web;
using BespokeDuaApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BespokeDuaApi.Auth;

public sealed class UserIdBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly BespokeDuaDbContext _db;

    public UserIdBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        BespokeDuaDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    public const string SchemeName = "UserIdBearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (!int.TryParse(token, out var userId) || userId <= 0)
        {
            return AuthenticateResult.Fail("Invalid token.");
        }

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return AuthenticateResult.Fail("User not found.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
