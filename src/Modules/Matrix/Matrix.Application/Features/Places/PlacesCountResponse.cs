namespace Matrix.Application.Features.Places;

public sealed class PlacesCountResponse
{
    [JsonPropertyName("count")]
    public long Count { get; init; }
}