namespace Matrix.Dto;

public sealed class PlacesCountResponse
{
    [JsonPropertyName("count")]
    public long Count { get; init; }
}