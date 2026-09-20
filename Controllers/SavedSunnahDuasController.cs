using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavedSunnahDuasController : ControllerBase
{
    private readonly BespokeDuaDbContext _context;

    public SavedSunnahDuasController(BespokeDuaDbContext context)
    {
        _context = context;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<SavedSunnahDuaDto>>> GetSavedSunnahDuasForUser(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            return NotFound("User not found.");

        var saved = await _context.SavedSunnahDuas
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new SavedSunnahDuaDto
            {
                SunnahDuaId = d.SunnahDuaId,
                SunnahDua = d.SunnahDua,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(saved);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedSunnahDuaDto>> GetSavedSunnahDua(Guid id)
    {
        var saved = await _context.SavedSunnahDuas
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.SunnahDuaId == id);

        if (saved is null)
            return NotFound();

        return Ok(new SavedSunnahDuaDto
        {
            SunnahDuaId = saved.SunnahDuaId,
            SunnahDua = saved.SunnahDua,
            CreatedAt = saved.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<SavedSunnahDuaDto>> CreateSavedSunnahDua(CreateSavedSunnahDuaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SunnahDua))
            return BadRequest("sunnahDua is required.");

        var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
            return NotFound("User not found.");

        var saved = new SavedSunnahDua
        {
            SunnahDuaId = Guid.NewGuid(),
            UserId = dto.UserId,
            SunnahDua = dto.SunnahDua,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedSunnahDuas.Add(saved);
        await _context.SaveChangesAsync();

        var response = new SavedSunnahDuaDto
        {
            SunnahDuaId = saved.SunnahDuaId,
            SunnahDua = saved.SunnahDua,
            CreatedAt = saved.CreatedAt
        };

        return CreatedAtAction(nameof(GetSavedSunnahDua), new { id = saved.SunnahDuaId }, response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSavedSunnahDua(Guid id)
    {
        var saved = await _context.SavedSunnahDuas.FindAsync(id);
        if (saved is null)
            return NotFound();

        _context.SavedSunnahDuas.Remove(saved);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
