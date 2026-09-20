using System.Text.Json.Serialization;

namespace BespokeDuaApi.Models
{
    public class DuaRequest
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("userId")]
        public int? UserId { get; set; }
    }
}