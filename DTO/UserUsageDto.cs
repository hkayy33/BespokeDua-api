namespace BespokeDuaApi.DTO
{
    public class UserUsageDto
    {
        public int DailyRequests { get; set; }
        public int MonthlyRequests { get; set; }
        public DateTime? LastRequestDate { get; set; }
        public string? Plan { get; set; }
    }
}