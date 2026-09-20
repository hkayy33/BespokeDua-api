using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PushDevicesController : ControllerBase
{
    private readonly BespokeDuaDbContext _context;

    public PushDevicesController(BespokeDuaDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterPushDeviceDto dto)
    {
        var token = NormalizeToken(dto.DeviceToken);
        if (dto.UserId <= 0 || token.Length < 32)
            return BadRequest(new { message = "A user id and device token are required." });

        var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
            return NotFound(new { message = "User not found." });

        var existing = await _context.DevicePushTokens.FirstOrDefaultAsync(d => d.Token == token);
        if (existing is null)
        {
            _context.DevicePushTokens.Add(new DevicePushToken
            {
                DevicePushTokenId = Guid.NewGuid(),
                UserId = dto.UserId,
                Token = token,
                IsSandbox = dto.Sandbox,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.UserId = dto.UserId;
            existing.IsSandbox = dto.Sandbox;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeToken(string? token) =>
        new string((token ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
}
