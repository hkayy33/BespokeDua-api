namespace BespokeDuaApi.Models;

public class AllahName
{
    public int Number { get; set; }
    public string Arabic { get; set; } = string.Empty;
    public string Transliteration { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public int FeelingLabelId { get; set; }
    public int SortOrder { get; set; }

    public FeelingLabel FeelingLabel { get; set; } = null!;
}
