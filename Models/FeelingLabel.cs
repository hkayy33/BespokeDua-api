namespace BespokeDuaApi.Models;

public class FeelingLabel
{
    public int FeelingLabelId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<AllahName> Names { get; set; } = [];
}
