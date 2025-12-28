namespace Contracts.Dto;

public sealed class InviteDataResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;
    
    [JsonPropertyName("program")]
    public int Program { get; init; }
    
    [JsonPropertyName("next_ref_no")]
    public int NextRefNo { get; init; }
    
    [JsonPropertyName("number")]
    public int Number { get; init; }
    
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
    public long SetAt { get; init; }
}
