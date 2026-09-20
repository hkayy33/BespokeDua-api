namespace BespokeDuaApi.DTO;

public class RegisterPushDeviceDto
{
    public int UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public bool Sandbox { get; set; }
}
