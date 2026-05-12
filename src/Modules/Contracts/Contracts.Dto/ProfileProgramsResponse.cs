namespace Contracts.Dto;

public sealed class ProfileProgramsResponse : List<Dictionary<string, ProgramDataResponse>>
{
}

public sealed class ProgramDataResponse
{
    [JsonPropertyName("inviter_addr")]
    public string InviterAddr { get; init; } = null!;
    
    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }
    
    [JsonPropertyName("invite_addr")]
    public string InviteAddr { get; init; } = null!;
    
    [JsonPropertyName("confirmed")]
    public uint Confirmed { get; init; }
}