namespace BespokeDuaApi.DTO
{
    public class CreateDuaCollectionDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid> DuaIds { get; set; } = new();
    }
}
