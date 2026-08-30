namespace ReferalProgram.Dto;

public sealed class ReferalProgramResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [JsonPropertyName("is_task_processing_enabled")]
    public bool IsTaskProcessingEnabled { get; init; }
}
