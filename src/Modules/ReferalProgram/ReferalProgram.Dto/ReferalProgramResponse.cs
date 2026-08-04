namespace ReferalProgram.Dto;

public sealed class ReferalProgramResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
}
