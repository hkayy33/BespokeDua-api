using System.Security.Claims;
using BespokeDuaApi.Data;
using BespokeDuaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Services;

public class AppUserService
{
    private readonly BespokeDuaDbContext _db;

    public AppUserService(BespokeDuaDbContext db)
    {
        _db = db;
    }

    public Guid? GetAuthUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var authUserId) ? authUserId : null;
    }

    public string? GetEmail(ClaimsPrincipal principal) =>
        principal.FindFirst("email")?.Value
        ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    public async Task<User?> FindByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.AuthUserId == authUserId, cancellationToken);

    public bool IsLegacyPrincipal(ClaimsPrincipal principal) =>
        string.Equals(principal.FindFirst("auth_mode")?.Value, "legacy", StringComparison.Ordinal);

    public int? GetLegacyUserId(ClaimsPrincipal principal)
    {
        if (!IsLegacyPrincipal(principal))
        {
            return null;
        }

        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : null;
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var legacyUserId = GetLegacyUserId(principal);
        if (legacyUserId is int id)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.UserId == id && u.AuthUserId == null, cancellationToken);
        }

        var authUserId = GetAuthUserId(principal);
        if (authUserId is null)
        {
            return null;
        }

        return await FindByAuthUserIdAsync(authUserId.Value, cancellationToken);
    }
}
