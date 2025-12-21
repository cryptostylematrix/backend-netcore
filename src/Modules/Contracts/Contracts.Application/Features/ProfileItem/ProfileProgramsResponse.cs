namespace Contracts.Application.Features.ProfileItem;

public sealed class ProfileProgramsResponse
{
    [JsonPropertyName("multi")]
    public ProgramDataResponse? Multi { get; init; }
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