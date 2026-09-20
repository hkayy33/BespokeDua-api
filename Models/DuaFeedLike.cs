namespace BespokeDuaApi.Models;

public class DuaFeedLike
{
    public Guid LikeId { get; set; }
    public Guid PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public DuaFeedPost Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
