namespace BespokeDuaApi.DTO
{
    public class UpdateDuaCollectionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid> DuaIds { get; set; } = new();
    }
}
