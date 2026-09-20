using Microsoft.EntityFrameworkCore;
using BespokeDuaApi.Data;
using BespokeDuaApi.Models;

namespace BespokeDuaApi.Services
{
    public class UsageService
    {
        private readonly BespokeDuaDbContext _context;

        public UsageService(BespokeDuaDbContext context)
        {
            _context = context;
        }

        private static DateTime UtcToday => DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        private static DateTime MonthStartUtc(DateTime utcDay)
        {
            return new DateTime(utcDay.Year, utcDay.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public async Task IncrementUsageAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var today = UtcToday;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var usage = await _context.UserUsages
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Date == today);

            if (usage == null)
            {
                usage = new UserUsage
                {
                    UserId = userId,
                    Date = today,
                    RequestsCount = 1
                };

                _context.UserUsages.Add(usage);
            }
            else
            {
                usage.RequestsCount++;
            }

            user.LastRequestDate = now;
            user.DailyRequests = usage.RequestsCount;

            var monthStart = MonthStartUtc(today);
            var monthlyBeforeToday = await _context.UserUsages
                .Where(u => u.UserId == userId && u.Date >= monthStart && u.Date < today)
                .SumAsync(u => (int?)u.RequestsCount) ?? 0;
            user.MonthlyRequests = monthlyBeforeToday + usage.RequestsCount;

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetDailyUsageAsync(int userId)
        {
            var today = UtcToday;

            return await _context.UserUsages
                .Where(u => u.UserId == userId && u.Date == today)
                .Select(u => u.RequestsCount)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetMonthlyUsageAsync(int userId)
        {
            var today = UtcToday;
            var monthStart = MonthStartUtc(today);

            return await _context.UserUsages
                .Where(u => u.UserId == userId && u.Date >= monthStart && u.Date <= today)
                .SumAsync(u => (int?)u.RequestsCount) ?? 0;
        }
    }
}