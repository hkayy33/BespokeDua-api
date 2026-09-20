namespace BespokeDuaApi.Models
{
    public class UserUsage
    {
        public int Id { get; set; }                      // PK
        public int UserId { get; set; }                  // FK
        public DateTime Date { get; set; }               // one row per user per day
        public int RequestsCount { get; set; }

        public User User { get; set; } = null!;
    }
}