namespace BespokeDuaApi.Models;

public class SavedSunnahDua
{
    public Guid SunnahDuaId { get; set; }
    public int UserId { get; set; }
    /// <summary>JSON payload (SunnahDuaItemDto fields: id, category, title, arabic, etc.).</summary>
    public string SunnahDua { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<DuaCollectionItem> CollectionItems { get; set; } = new List<DuaCollectionItem>();
}
