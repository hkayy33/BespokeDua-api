using BespokeDuaApi.DTO;
using BespokeDuaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BespokeDuaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsageController : ControllerBase
    {
        private readonly UsageService _usageService;

        public UsageController(UsageService usageService)
        {
            _usageService = usageService;
        }

        /// <summary>
        /// Increments today's request count for the user and updates LastRequestDate.
        /// </summary>
        [HttpPost("record")]
        public async Task<IActionResult> RecordUsage([FromBody] RecordUsageDto dto)
        {
            if (dto == null || dto.UserId <= 0)
            {
                return BadRequest(new { message = "A valid UserId is required." });
            }

            try
            {
                await _usageService.IncrementUsageAsync(dto.UserId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }

            return NoContent();
        }
    }
}
