namespace BespokeDuaApi.DTO
{
    public class SavedDuaDto
    {
        public Guid DuaId { get; set; }
        public string? Dua { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}