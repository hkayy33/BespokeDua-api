namespace BespokeDuaApi.DTO
{
    public class DuaCollectionDto
    {
        public Guid CollectionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<SavedDuaDto> SavedDuas { get; set; } = new();
    }
}
