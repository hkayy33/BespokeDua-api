using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;

namespace BespokeDuaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavedDuasController : ControllerBase
    {
        private readonly BespokeDuaDbContext _context;

        public SavedDuasController(BespokeDuaDbContext context)
        {
            _context = context;
        }

        // GET: api/SavedDuas/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<SavedDuaDto>>> GetSavedDuasForUser(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);

            if (!userExists)
            {
                return NotFound("User not found.");
            }

            var savedDuas = await _context.SavedDuas
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new SavedDuaDto
                {
                    DuaId = d.DuaId,
                    Dua = d.Dua,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync();

            return Ok(savedDuas);
        }

        // GET: api/SavedDuas/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SavedDuaDto>> GetSavedDua(Guid id)
        {
            var savedDua = await _context.SavedDuas
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DuaId == id);

            if (savedDua == null)
            {
                return NotFound();
            }

            var dto = new SavedDuaDto
            {
                DuaId = savedDua.DuaId,
                Dua = savedDua.Dua,
                CreatedAt = savedDua.CreatedAt,
                UpdatedAt = savedDua.UpdatedAt
            };

            return Ok(dto);
        }

        // POST: api/SavedDuas
        [HttpPost]
        public async Task<ActionResult<SavedDuaDto>> CreateSavedDua(CreateSavedDuaDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);

            if (!userExists)
            {
                return NotFound("User not found.");
            }

            var savedDua = new SavedDua
            {
                DuaId = Guid.NewGuid(),
                UserId = dto.UserId,
                Dua = dto.Dua,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavedDuas.Add(savedDua);
            await _context.SaveChangesAsync();

            var savedDuaDto = new SavedDuaDto
            {
                DuaId = savedDua.DuaId,
                Dua = savedDua.Dua,
                CreatedAt = savedDua.CreatedAt,
                UpdatedAt = savedDua.UpdatedAt
            };

            return CreatedAtAction(nameof(GetSavedDua), new { id = savedDua.DuaId }, savedDuaDto);
        }

        // PUT: api/SavedDuas/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<SavedDuaDto>> UpdateSavedDua(Guid id, UpdateSavedDuaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Dua))
            {
                return BadRequest("Dua is required.");
            }

            var savedDua = await _context.SavedDuas.FindAsync(id);

            if (savedDua == null)
            {
                return NotFound();
            }

            savedDua.Dua = dto.Dua;
            savedDua.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var savedDuaDto = new SavedDuaDto
            {
                DuaId = savedDua.DuaId,
                Dua = savedDua.Dua,
                CreatedAt = savedDua.CreatedAt,
                UpdatedAt = savedDua.UpdatedAt
            };

            return Ok(savedDuaDto);
        }

        // DELETE: api/SavedDuas/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteSavedDua(Guid id)
        {
            var savedDua = await _context.SavedDuas.FindAsync(id);

            if (savedDua == null)
            {
                return NotFound();
            }

            _context.SavedDuas.Remove(savedDua);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}