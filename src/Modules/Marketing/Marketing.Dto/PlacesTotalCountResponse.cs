namespace Marketing.Dto;

public sealed class PlacesTotalCountResponse
{
    [JsonPropertyName("total_count")]
    public long TotalCount { get; init; }
}