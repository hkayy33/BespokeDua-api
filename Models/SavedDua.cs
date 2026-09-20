using System;

namespace BespokeDuaApi.Models
{
    public class SavedDua
    {
        public Guid DuaId { get; set; }                  // PK
        public int UserId { get; set; }                  // FK
        public string Dua { get; set; } = string.Empty;  // JSON string
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<DuaCollectionItem> CollectionItems { get; set; } = new List<DuaCollectionItem>();
    }
}