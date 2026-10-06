namespace ReferalProgram.Dto;

public sealed class ActivationOptionResponse
{
    [JsonPropertyName("can_activate")]
    public bool CanActivate { get; init; }
    [JsonPropertyName("command_tag")]
    public uint? CommandTag { get; init; }
    [JsonPropertyName("structure_number")]
    public byte? StructureNumber { get; init; }
    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }
    [JsonPropertyName("place_number")]
    public uint? PlaceNumber { get; init; }
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
