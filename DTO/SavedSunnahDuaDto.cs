namespace BespokeDuaApi.DTO;

public class SavedSunnahDuaDto
{
    public Guid SunnahDuaId { get; set; }
    public string SunnahDua { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSavedSunnahDuaDto
{
    public int UserId { get; set; }
    public string SunnahDua { get; set; } = string.Empty;
}
