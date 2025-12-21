namespace Contracts.Application.Features.ProfileItem;

public sealed class ProfileDataResponse
{
    [JsonPropertyName("is_init")]
    public int IsInit { get; init; }
    
    [JsonPropertyName("index")]
    public string Index { get; init; } = null!;
    
    [JsonPropertyName("collection_addr")]
    public string CollectionAddr { get; init; } = null!;
    
    [JsonPropertyName("owner_addr")]
    public string? OwnerAddr { get; init; }
    
    [JsonPropertyName("content")]
    public ProfileContentResponse? Content { get; init; }
}

public sealed class ProfileContentResponse
{
    [JsonPropertyName("login")]
    public string Login { get; init; } = null!;
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; init; }
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }
    
    [JsonPropertyName("tg_username")]
    public string? TgUsername { get; init; }
}