namespace ReferalProgram.Dto;

public sealed class StructureResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [JsonPropertyName("structure_number")]
    public byte StructureNumber { get; init; }

    [JsonPropertyName("max_places_per_profile")]
    public int MaxPlacesPerProfile { get; init; }

    [JsonPropertyName("width")]
    public byte Width { get; init; }

    [JsonPropertyName("height")]
    public byte Height { get; init; }

    [JsonPropertyName("display_height")]
    public byte DisplayHeight { get; init; }

    [JsonPropertyName("prev_required")]
    public bool PrevRequired { get; init; }

    [JsonPropertyName("pos_algo")]
    public System.Text.Json.JsonElement PosAlgo { get; init; }

    private string? _group;

    [JsonPropertyName("group")]
    public string? Group
    {
        get => _group;
        init => _group = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [JsonPropertyName("activity")]
    public System.Text.Json.JsonElement? Activity { get; init; }
}
