namespace BespokeDuaApi.DTO;

public class SunnahDuaMatchResponse
{
    public string UserInput { get; set; } = string.Empty;
    public SunnahDuaCategoryMatchDto[] Categories { get; set; } = [];
}

public class SunnahDuaCategoryMatchDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public SunnahDuaItemDto[] Duas { get; set; } = [];
}

public class SunnahDuaItemDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Arabic { get; set; } = string.Empty;
    public string Transliteration { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int Repeat { get; set; }
}

public class SunnahDuaCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
}
