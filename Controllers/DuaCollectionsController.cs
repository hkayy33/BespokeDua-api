using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DuaCollectionsController : ControllerBase
{
    private readonly BespokeDuaDbContext _context;

    public DuaCollectionsController(BespokeDuaDbContext context)
    {
        _context = context;
    }

    // GET: api/DuaCollections/user/5
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<DuaCollectionSummaryDto>>> GetCollectionsForUser(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            return NotFound("User not found.");

        var collections = await _context.DuaCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new DuaCollectionSummaryDto
            {
                CollectionId = c.CollectionId,
                Name = c.Name,
                Description = c.Description,
                DuaCount = c.Items.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(collections);
    }

    // GET: api/DuaCollections/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DuaCollectionDto>> GetCollection(Guid id)
    {
        var collection = await LoadCollectionDtoAsync(id);
        if (collection is null)
            return NotFound();

        return Ok(collection);
    }

    // POST: api/DuaCollections
    [HttpPost]
    public async Task<ActionResult<DuaCollectionDto>> CreateCollection(CreateDuaCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
            return NotFound("User not found.");

        var distinctDuaIds = dto.DuaIds.Distinct().ToList();
        var (validationError, items) = await BuildCollectionItemsAsync(dto.UserId, distinctDuaIds);
        if (validationError is not null)
            return BadRequest(validationError);

        var now = DateTime.UtcNow;
        var collection = new DuaCollection
        {
            CollectionId = Guid.NewGuid(),
            UserId = dto.UserId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = items
        };

        _context.DuaCollections.Add(collection);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCollection),
            new { id = collection.CollectionId },
            await LoadCollectionDtoAsync(collection.CollectionId));
    }

    // PUT: api/DuaCollections/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DuaCollectionDto>> UpdateCollection(Guid id, UpdateDuaCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var collection = await _context.DuaCollections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CollectionId == id);

        if (collection is null)
            return NotFound();

        var distinctDuaIds = dto.DuaIds.Distinct().ToList();
        var (validationError, items) = await BuildCollectionItemsAsync(collection.UserId, distinctDuaIds);
        if (validationError is not null)
            return BadRequest(validationError);

        collection.Name = dto.Name.Trim();
        collection.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        collection.UpdatedAt = DateTime.UtcNow;

        _context.DuaCollectionItems.RemoveRange(collection.Items);
        foreach (var item in items)
            item.CollectionId = collection.CollectionId;
        collection.Items = items;

        await _context.SaveChangesAsync();

        return Ok(await LoadCollectionDtoAsync(collection.CollectionId));
    }

    // DELETE: api/DuaCollections/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCollection(Guid id)
    {
        var collection = await _context.DuaCollections.FindAsync(id);
        if (collection is null)
            return NotFound();

        _context.DuaCollections.Remove(collection);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<(string? Error, List<DuaCollectionItem> Items)> BuildCollectionItemsAsync(
        int userId,
        IReadOnlyList<Guid> duaIds)
    {
        if (duaIds.Count == 0)
            return (null, new List<DuaCollectionItem>());

        var ownedSavedDuaIds = await _context.SavedDuas
            .AsNoTracking()
            .Where(d => d.UserId == userId && duaIds.Contains(d.DuaId))
            .Select(d => d.DuaId)
            .ToListAsync();

        var ownedSunnahDuaIds = await _context.SavedSunnahDuas
            .AsNoTracking()
            .Where(d => d.UserId == userId && duaIds.Contains(d.SunnahDuaId))
            .Select(d => d.SunnahDuaId)
            .ToListAsync();

        var ownedIds = ownedSavedDuaIds
            .Concat(ownedSunnahDuaIds)
            .ToHashSet();

        if (ownedIds.Count != duaIds.Count)
            return ("One or more saved duas were not found for this user.", new List<DuaCollectionItem>());

        var savedDuaIdSet = ownedSavedDuaIds.ToHashSet();
        var items = duaIds
            .Select((duaId, index) => new DuaCollectionItem
            {
                ItemId = Guid.NewGuid(),
                DuaId = savedDuaIdSet.Contains(duaId) ? duaId : null,
                SunnahDuaId = savedDuaIdSet.Contains(duaId) ? null : duaId,
                SortOrder = index
            })
            .ToList();

        return (null, items);
    }

    private async Task<DuaCollectionDto?> LoadCollectionDtoAsync(Guid collectionId)
    {
        return await _context.DuaCollections
            .AsNoTracking()
            .Where(c => c.CollectionId == collectionId)
            .Select(c => new DuaCollectionDto
            {
                CollectionId = c.CollectionId,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                SavedDuas = c.Items
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new SavedDuaDto
                    {
                        DuaId = i.DuaId ?? i.SunnahDuaId!.Value,
                        Dua = i.DuaId != null ? i.SavedDua!.Dua : i.SavedSunnahDua!.SunnahDua,
                        CreatedAt = i.DuaId != null ? i.SavedDua!.CreatedAt : i.SavedSunnahDua!.CreatedAt,
                        UpdatedAt = i.DuaId != null ? i.SavedDua!.UpdatedAt : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }
}
