namespace BespokeDuaApi.Models
{
    public class User
    {
        public int UserId { get; set; }                  // PK
        /// <summary>Supabase <c>auth.users.id</c> (JWT <c>sub</c>).</summary>
        public Guid? AuthUserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? HashedPassword { get; set; }
        public PlanType Plan { get; set; } = PlanType.Free;
        public int DailyRequests { get; set; }
        public int MonthlyRequests { get; set; }
        public DateTime? LastRequestDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<SavedDua> SavedDuas { get; set; } = new List<SavedDua>();
        public ICollection<SavedSunnahDua> SavedSunnahDuas { get; set; } = new List<SavedSunnahDua>();
        public ICollection<DuaCollection> DuaCollections { get; set; } = new List<DuaCollection>();
        public ICollection<DuaFeedPost> DuaFeedPosts { get; set; } = new List<DuaFeedPost>();
        public ICollection<DuaFeedLike> DuaFeedLikes { get; set; } = new List<DuaFeedLike>();
        public ICollection<DevicePushToken> DevicePushTokens { get; set; } = new List<DevicePushToken>();
    }
}