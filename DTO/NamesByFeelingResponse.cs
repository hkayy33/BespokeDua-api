namespace BespokeDuaApi.DTO;

public class NamesByFeelingResponse
{
    public int FeelingLabelId { get; set; }
    public string FeelingLabel { get; set; } = string.Empty;
    public NameByFeelingDto[] Names { get; set; } = [];
}
