namespace BespokeDuaApi.DTO
{
    public class DuaCollectionSummaryDto
    {
        public Guid CollectionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DuaCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
