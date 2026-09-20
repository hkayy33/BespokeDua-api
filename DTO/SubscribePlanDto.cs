namespace BespokeDuaApi.DTO;

/// <summary>Body for <c>PATCH api/Plan/subscribe</c> (matches iOS <c>SubscribePlanRequest</c>).</summary>
public class SubscribePlanDto
{
    public string? OriginalTransactionId { get; set; }
    public string? ProductId { get; set; }
    public bool ConfirmTransfer { get; set; }
}
