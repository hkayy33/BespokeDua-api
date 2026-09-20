namespace BespokeDuaApi.Models
{
    public class DuaCollection
    {
        public Guid CollectionId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<DuaCollectionItem> Items { get; set; } = new List<DuaCollectionItem>();
    }
}
