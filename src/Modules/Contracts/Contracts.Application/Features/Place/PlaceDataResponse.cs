namespace Contracts.Application.Features.Place;

public sealed class PlaceDataResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public uint M { get; init; }
    
    [JsonPropertyName("parent_addr")]
    public string? ParentAddr { get; init; }
    
    [JsonPropertyName("created_at")]
    public ulong CreatedAt { get; init; }
    
    [JsonPropertyName("fill_count")]
    public uint FillCount { get; init; }

    [JsonPropertyName("profiles")]
    public PlaceProfilesResponse Profiles { get; init; } = null!;
    
    [JsonPropertyName("security")]
    public PlaceSecurityResponse Security { get; init; } = null!;
    
    [JsonPropertyName("children")]
    public PlaceChildrenResponse? Children { get; init; }
}

public sealed class PlaceProfilesResponse
{
    [JsonPropertyName("clone")]
    public uint Clone { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }
    
    [JsonPropertyName("inviter_profile_addr")]
    public string? InviterProfileAddr { get; init; }
}

public sealed class PlaceSecurityResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;
}

public sealed class PlaceChildrenResponse
{
    [JsonPropertyName("left_addr")]
    public string LeftAddr { get; init; } = null!;
    
    [JsonPropertyName("right_addr")]
    public string? RightAddr { get; init; }
}