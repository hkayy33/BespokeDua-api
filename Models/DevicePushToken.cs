namespace BespokeDuaApi.Models;

public class DevicePushToken
{
    public Guid DevicePushTokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsSandbox { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
