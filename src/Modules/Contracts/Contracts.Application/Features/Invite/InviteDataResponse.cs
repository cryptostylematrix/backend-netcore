namespace Contracts.Application.Features.Invite;

public sealed class InviteDataResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;
    
    [JsonPropertyName("program")]
    public uint Program { get; init; }
    
    [JsonPropertyName("next_ref_no")]
    public uint NextRefNo { get; init; }
    
    [JsonPropertyName("number")]
    public uint Number { get; init; }
    
    [JsonPropertyName("parent_addr")]
    public string? ParentAddr { get; init; }
    
    [JsonPropertyName("owner")]
    public InviteOwnerResponse? Owner { get; init; }
}

public sealed class InviteOwnerResponse
{
    [JsonPropertyName("owner_addr")]
    public string OwnerAddr { get; init; } = null!;
    
    [JsonPropertyName("set_at")]
    public ulong SetAt { get; init; }
}
