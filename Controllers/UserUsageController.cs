using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;

namespace BespokeDuaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserUsageController : ControllerBase
    {
        private readonly BespokeDuaDbContext _context;

        public UserUsageController(BespokeDuaDbContext context)
        {
            _context = context;
        }

        // GET: api/UserUsage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserUsageDto>> GetUserUsage(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dailyRequests = await _context.UserUsages
                .AsNoTracking()
                .Where(x => x.UserId == id && x.Date == today)
                .Select(x => x.RequestsCount)
                .FirstOrDefaultAsync();

            var monthlyRequests = await _context.UserUsages
                .AsNoTracking()
                .Where(x => x.UserId == id && x.Date >= monthStart && x.Date <= today)
                .SumAsync(x => (int?)x.RequestsCount) ?? 0;

            var userUsageDto = new UserUsageDto
            {
                DailyRequests = dailyRequests,
                MonthlyRequests = monthlyRequests,
                LastRequestDate = user.LastRequestDate,
                Plan = user.Plan.ToString()
            };

            return Ok(userUsageDto);
        }
    }
}