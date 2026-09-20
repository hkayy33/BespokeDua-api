namespace BespokeDuaApi.Models;

public class DuaFeedPost
{
    public Guid PostId { get; set; }
    public int UserId { get; set; }
    public Guid? SavedDuaId { get; set; }
    public Guid? SavedSunnahDuaId { get; set; }
    /// <summary>Snapshot of the saved dua JSON at post time.</summary>
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;
    public SavedDua? SavedDua { get; set; }
    public SavedSunnahDua? SavedSunnahDua { get; set; }
    public ICollection<DuaFeedLike> Likes { get; set; } = new List<DuaFeedLike>();
}
