namespace BespokeDuaApi.DTO
{
    public class GetUserDto
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Plan { get; set; }
        public DateTime? LastRequestDate { get; set; }
    }
}