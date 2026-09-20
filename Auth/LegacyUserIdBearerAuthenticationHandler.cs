using System.Security.Claims;
using System.Text.Encodings.Web;
using BespokeDuaApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BespokeDuaApi.Auth;

/// <summary>
/// Legacy auth for users created before Supabase (Bearer token is numeric <see cref="Models.User.UserId"/>).
/// </summary>
public sealed class LegacyUserIdBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly BespokeDuaDbContext _db;

    public LegacyUserIdBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        BespokeDuaDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    public const string SchemeName = "LegacyUserIdBearer";

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
            return AuthenticateResult.NoResult();
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null || user.AuthUserId is not null)
        {
            return AuthenticateResult.Fail("Invalid legacy token.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("auth_mode", "legacy"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
