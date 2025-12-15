namespace Contracts.Application.Features.Place;

public sealed class PlaceDataResponse
{
    public string MarketingAddr { get; init; } = null!;
    public uint M { get; init; }
    public string? ParentAddr { get; init; }
    public ulong CreatedAt { get; init; }
    public uint FillCount { get; init; }

    public PlaceProfilesResponse Profiles { get; init; } = null!;
    public PlaceSecurityResponse Security { get; init; } = null!;
    public PlaceChildrenResponse? Children { get; init; }
}

public sealed class PlaceProfilesResponse
{
    public uint Clone { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public uint PlaceNumber { get; init; }
    public string? InviterProfileAddr { get; init; }
}

public sealed class PlaceSecurityResponse
{
    public string AdminAddr { get; init; } = null!;
}

public sealed class PlaceChildrenResponse
{
    public string LeftAddr { get; init; } = null!;
    public string? RightAddr { get; init; }
}