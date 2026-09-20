using System.Text.Json.Serialization;

namespace BespokeDuaApi.Models;

public class SunnahDuaRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    /// <summary>Max categories Gemini may select (default 3).</summary>
    [JsonPropertyName("maxCategories")]
    public int? MaxCategories { get; set; }

    /// <summary>Duas returned per matched category (default 3).</summary>
    [JsonPropertyName("duasPerCategory")]
    public int? DuasPerCategory { get; set; }
}
