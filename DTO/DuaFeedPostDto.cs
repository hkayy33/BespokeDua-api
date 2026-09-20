namespace BespokeDuaApi.DTO;

public class DuaFeedPostDto
{
    public Guid PostId { get; set; }
    /// <summary>Null when <see cref="IsAnonymous"/> is true.</summary>
    public string? AuthorUsername { get; set; }
    public bool IsAnonymous { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DuaCount { get; set; }
    public bool HasUserMadeDua { get; set; }
    /// <summary>True when the requesting user (feed <c>userId</c> query param) authored this post.</summary>
    public bool IsOwnPost { get; set; }
}

public class DuaFeedPageDto
{
    public List<DuaFeedPostDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class CreateDuaFeedPostDto
{
    public int UserId { get; set; }
    public Guid SavedDuaId { get; set; }
    public bool IsAnonymous { get; set; }
}

public class MakeDuaDto
{
    public int UserId { get; set; }
}

public class MakeDuaResultDto
{
    public int DuaCount { get; set; }
    public bool HasUserMadeDua { get; set; }
}

public class DuaFeedPostingQuotaDto
{
    public int UsedToday { get; set; }
    public int DailyLimit { get; set; }
    public int RemainingToday { get; set; }
}
