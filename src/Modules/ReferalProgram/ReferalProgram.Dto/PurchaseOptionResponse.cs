namespace ReferalProgram.Dto;

public sealed class PurchaseOptionResponse
{
    [JsonPropertyName("can_buy")]
    public bool CanBuy { get; init; }

    [JsonPropertyName("command_tag")]
    public uint? CommandTag { get; init; }

    [JsonPropertyName("include_position")]
    public bool IncludePosition { get; init; }

    [JsonPropertyName("position")]
    public NextPosResponse? Position { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
