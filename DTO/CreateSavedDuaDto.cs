namespace BespokeDuaApi.DTO
{
    public class CreateSavedDuaDto
    {
        public int UserId { get; set; }
        public string Dua { get; set; } = string.Empty;
    }
}