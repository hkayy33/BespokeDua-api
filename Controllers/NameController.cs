using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/names")]
public class NameController : ControllerBase
{
    private readonly BespokeDuaDbContext _context;

    public NameController(BespokeDuaDbContext context)
    {
        _context = context;
    }

    [HttpGet("feeling-labels")]
    public async Task<ActionResult<IEnumerable<FeelingLabelDto>>> GetFeelingLabels()
    {
        var labels = await _context.FeelingLabels
            .AsNoTracking()
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new FeelingLabelDto
            {
                FeelingLabelId = f.FeelingLabelId,
                Label = f.Label,
                DisplayOrder = f.DisplayOrder
            })
            .ToListAsync();

        return Ok(labels);
    }

    [HttpGet("by-feeling/{feelingLabelId:int}")]
    public async Task<ActionResult<NamesByFeelingResponse>> GetNamesByFeelingId(int feelingLabelId)
    {
        var label = await _context.FeelingLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FeelingLabelId == feelingLabelId);

        if (label is null)
            return NotFound("Feeling label not found.");

        var names = await LoadNamesByFeelingAsync(label.FeelingLabelId);

        return Ok(new NamesByFeelingResponse
        {
            FeelingLabelId = label.FeelingLabelId,
            FeelingLabel = label.Label,
            Names = names
        });
    }

    [HttpGet("by-feeling")]
    public async Task<ActionResult<NamesByFeelingResponse>> GetNamesByFeelingLabel(
        [FromQuery] string feelingLabel)
    {
        if (string.IsNullOrWhiteSpace(feelingLabel))
            return BadRequest("feelingLabel is required.");

        var label = await _context.FeelingLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Label == feelingLabel.Trim());

        if (label is null)
            return NotFound("Feeling label not found.");

        var names = await LoadNamesByFeelingAsync(label.FeelingLabelId);

        return Ok(new NamesByFeelingResponse
        {
            FeelingLabelId = label.FeelingLabelId,
            FeelingLabel = label.Label,
            Names = names
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AllahNameDto>>> GetNames(
        [FromQuery] int? feelingLabelId,
        [FromQuery] string? feelingLabel)
    {
        if (feelingLabelId is null && string.IsNullOrWhiteSpace(feelingLabel))
            return BadRequest("Provide feelingLabelId or feelingLabel.");

        var query = _context.AllahNames
            .AsNoTracking()
            .Include(n => n.FeelingLabel)
            .AsQueryable();

        if (feelingLabelId is int id)
        {
            var exists = await _context.FeelingLabels.AnyAsync(f => f.FeelingLabelId == id);
            if (!exists)
                return NotFound("Feeling label not found.");

            query = query.Where(n => n.FeelingLabelId == id);
        }
        else
        {
            var label = feelingLabel!.Trim();
            var match = await _context.FeelingLabels
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Label == label);

            if (match is null)
                return NotFound("Feeling label not found.");

            query = query.Where(n => n.FeelingLabelId == match.FeelingLabelId);
        }

        var names = await query
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Number)
            .Select(n => new AllahNameDto
            {
                Number = n.Number,
                Arabic = n.Arabic,
                Transliteration = n.Transliteration,
                Translation = n.Translation,
                Meaning = n.Meaning,
                FeelingLabelId = n.FeelingLabelId,
                FeelingLabel = n.FeelingLabel.Label,
                SortOrder = n.SortOrder
            })
            .ToListAsync();

        return Ok(names);
    }

    [HttpGet("{number:int}")]
    public async Task<ActionResult<AllahNameDto>> GetName(int number)
    {
        if (number is < 1 or > 99)
            return BadRequest("Name number must be between 1 and 99.");

        var name = await _context.AllahNames
            .AsNoTracking()
            .Include(n => n.FeelingLabel)
            .Where(n => n.Number == number)
            .Select(n => new AllahNameDto
            {
                Number = n.Number,
                Arabic = n.Arabic,
                Transliteration = n.Transliteration,
                Translation = n.Translation,
                Meaning = n.Meaning,
                FeelingLabelId = n.FeelingLabelId,
                FeelingLabel = n.FeelingLabel.Label,
                SortOrder = n.SortOrder
            })
            .FirstOrDefaultAsync();

        if (name is null)
            return NotFound("Name not found.");

        return Ok(name);
    }

    private async Task<NameByFeelingDto[]> LoadNamesByFeelingAsync(int feelingLabelId)
    {
        return await _context.AllahNames
            .AsNoTracking()
            .Where(n => n.FeelingLabelId == feelingLabelId)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Number)
            .Select(n => new NameByFeelingDto
            {
                Name = n.Arabic,
                Arabic = n.Arabic,
                Transliteration = n.Transliteration,
                Translation = n.Translation,
                Meaning = n.Meaning
            })
            .ToArrayAsync();
    }
}
