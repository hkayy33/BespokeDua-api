namespace BespokeDuaApi.Models;

/// <summary>Maps Apple subscription (original transaction id) to a user. Table exists in Supabase as <c>SubscriptionOwnerships</c>.</summary>
public class SubscriptionOwnership
{
    public int SubscriptionOwnershipId { get; set; }
    public string OriginalTransactionId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
