using System.Text.Json.Serialization;

namespace Common.Dto;

public sealed class Paginated<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; init; } = null!;
    
    [JsonPropertyName("page")]
    public int Page { get; init; }
    
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; init; }
}