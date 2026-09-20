using BespokeDuaApi.Auth;
using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using BespokeDuaApi.Services;
using BespokeDuaApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BespokeDuaDbContext _context;
    private readonly AppUserService _appUsers;
    private readonly SupabaseAuthAdminService _supabaseAdmin;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        BespokeDuaDbContext context,
        AppUserService appUsers,
        SupabaseAuthAdminService supabaseAdmin,
        ILogger<AuthController> logger)
    {
        _context = context;
        _appUsers = appUsers;
        _supabaseAdmin = supabaseAdmin;
        _logger = logger;
    }

    /// <summary>
    /// True when this email already belongs to an app account. Checked before a
    /// verification sign-up so we never create a second identity for an existing user.
    /// </summary>
    [HttpGet("email-in-use")]
    public async Task<ActionResult> EmailInUse([FromQuery] string? email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var inUse = await _context.Users.AnyAsync(u => EF.Functions.ILike(u.Email, trimmed));
        return Ok(new { inUse });
    }

    [HttpPost("register")]
    public async Task<ActionResult<GetUserDto>> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Username, Email, and Password are required." });
        }

        var username = dto.Username.Trim();
        var email = dto.Email.Trim();

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return BadRequest(new { message = "Username already exists." });
        }

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return BadRequest(new { message = "Email already exists." });
        }

        var user = new User
        {
            Username = username,
            Email = email,
            HashedPassword = PasswordHasher.HashPassword(dto.Password),
            Plan = PlanType.Free,
            CreatedAt = DateTime.UtcNow,
            LastRequestDate = null,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCurrentUser), ToDto(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email and Password are required." });
        }

        var email = dto.Email.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email));

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (string.IsNullOrEmpty(user.HashedPassword) ||
            !PasswordHasher.VerifyPassword(dto.Password, user.HashedPassword))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // A stored password is the old sign-in. Don't force that account through email verification.
        if (user.AuthUserId is not null)
        {
            user.AuthUserId = null;
            await _context.SaveChangesAsync();
        }

        return Ok(new LoginResponseDto
        {
            Message = "Login successful.",
            User = ToDto(user),
        });
    }

    [Authorize(AuthenticationSchemes = SupabaseAuthExtensions.SchemeName)]
    [HttpPost("sync")]
    public async Task<ActionResult<GetUserDto>> SyncProfile(SyncProfileDto dto)
    {
        var authUserId = _appUsers.GetAuthUserId(User);
        if (authUserId is null)
        {
            return Unauthorized();
        }

        try
        {
            var email = _appUsers.GetEmail(User);
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email claim is missing from the token." });
            }

            var user = await _appUsers.FindByAuthUserIdAsync(authUserId.Value);
            if (user is not null)
            {
                var usernameError = await TryApplyEmailAndUsernameAsync(user, email, dto.Username, authUserId.Value);
                if (usernameError is not null)
                {
                    return BadRequest(new { message = usernameError });
                }

                await _context.SaveChangesAsync();
                return Ok(ToDto(user));
            }

            var existingByEmail = await _context.Users
                .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email));

            if (existingByEmail is not null)
            {
                if (!string.IsNullOrEmpty(existingByEmail.HashedPassword))
                {
                    return Conflict(new { message = "This email already has an account. Sign in with your password." });
                }

                if (existingByEmail.AuthUserId is not null && existingByEmail.AuthUserId != authUserId)
                {
                    return Conflict(new { message = "This email is already linked to another account." });
                }

                existingByEmail.AuthUserId = authUserId;
                var linkError = await TryApplyEmailAndUsernameAsync(existingByEmail, email, dto.Username, authUserId.Value);
                if (linkError is not null)
                {
                    return BadRequest(new { message = linkError });
                }

                await _context.SaveChangesAsync();
                return Ok(ToDto(existingByEmail));
            }

            var username = await ResolveAvailableUsernameAsync(dto.Username, email, exceptUserId: null);

            user = new User
            {
                AuthUserId = authUserId,
                Username = username,
                Email = email,
                Plan = PlanType.Free,
                CreatedAt = DateTime.UtcNow,
                LastRequestDate = null,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(ToDto(user));
        }
        catch (DbUpdateException ex)
        {
            var existingAfterRace = await _appUsers.FindByAuthUserIdAsync(authUserId.Value);
            if (existingAfterRace is not null)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrent profile sync for auth user {AuthUserId}; returning existing profile.",
                    authUserId);
                return Ok(ToDto(existingAfterRace));
            }

            _logger.LogError(ex, "Failed to sync Supabase profile for auth user {AuthUserId}.", authUserId);
            return Conflict(new { message = "Could not create your profile. The email or username may already be in use." });
        }
    }

    [Authorize(AuthenticationSchemes = AppAuthenticationExtensions.CombinedScheme)]
    [HttpPatch("username")]
    public async Task<ActionResult<GetUserDto>> UpdateUsername(UpdateUsernameDto dto)
    {
        var user = await _appUsers.GetCurrentUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new { message = "Profile not found." });
        }

        if (!UsernameValidator.TryValidate(dto.Username, out var normalized, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        if (string.Equals(user.Username, normalized, StringComparison.Ordinal))
        {
            return Ok(ToDto(user));
        }

        if (await _context.Users.AnyAsync(u => u.Username == normalized && u.UserId != user.UserId))
        {
            return BadRequest(new { message = "Username already exists." });
        }

        user.Username = normalized;
        await _context.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    [Authorize(AuthenticationSchemes = AppAuthenticationExtensions.CombinedScheme)]
    [HttpGet("me")]
    public async Task<ActionResult<GetUserDto>> GetCurrentUser()
    {
        var user = await _appUsers.GetCurrentUserAsync(User);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(ToDto(user));
    }

    [Authorize(AuthenticationSchemes = AppAuthenticationExtensions.CombinedScheme)]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await _appUsers.GetCurrentUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var authUserId = user.AuthUserId ?? _appUsers.GetAuthUserId(User);
        if (authUserId is Guid supabaseId)
        {
            if (!_supabaseAdmin.IsConfigured)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Account deletion is not fully configured on the server (missing Supabase service role key)."
                });
            }

            if (!_supabaseAdmin.IsKeyMatchedToProject)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message =
                        "Account deletion is misconfigured: the Supabase service role key does not match Supabase:Url. Update ServiceRoleKey in appsettings.json to the key from the same Supabase project."
                });
            }

            var deleteResult = await _supabaseAdmin.DeleteAuthUserAsync(supabaseId);
            if (deleteResult == SupabaseDeleteResult.InvalidConfiguration)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message =
                        "Account deletion is misconfigured: the Supabase service role key does not match Supabase:Url."
                });
            }

            if (deleteResult == SupabaseDeleteResult.Failed)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Could not remove your sign-in account. Please try again or contact support."
                });
            }
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> TryApplyEmailAndUsernameAsync(
        User user,
        string email,
        string? requestedUsername,
        Guid authUserId)
    {
        user.AuthUserId ??= authUserId;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
        }

        if (string.IsNullOrWhiteSpace(requestedUsername))
        {
            return null;
        }

        var username = requestedUsername.Trim();
        if (string.Equals(user.Username, username, StringComparison.Ordinal))
        {
            return null;
        }

        if (await _context.Users.AnyAsync(u => u.Username == username && u.UserId != user.UserId))
        {
            return "Username already exists.";
        }

        user.Username = username;
        return null;
    }

    private async Task<string> ResolveAvailableUsernameAsync(string? requestedUsername, string email, int? exceptUserId)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedUsername)
            ? UsernameHelper.FromEmail(email)
            : requestedUsername.Trim();

        return await EnsureUniqueUsernameAsync(baseName, exceptUserId);
    }

    private async Task<string> EnsureUniqueUsernameAsync(string baseName, int? exceptUserId)
    {
        var candidate = baseName;
        var suffix = 0;

        while (await _context.Users.AnyAsync(u =>
                   u.Username == candidate && (exceptUserId == null || u.UserId != exceptUserId)))
        {
            suffix++;
            var suffixText = suffix.ToString();
            var maxBase = Math.Max(1, 100 - suffixText.Length);
            candidate = baseName.Length > maxBase ? baseName[..maxBase] + suffixText : baseName + suffixText;
        }

        return candidate;
    }

    private static GetUserDto ToDto(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        Plan = user.Plan.ToString(),
        LastRequestDate = user.LastRequestDate,
    };
}
