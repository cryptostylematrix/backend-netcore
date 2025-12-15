namespace Contracts.Domain.Aggregates.Place;

public sealed class PlaceData
{
    public Address Marketing { get; init; } = null!;
    public BigInteger M { get; init; }
    public Address? Parent { get; init; }
    public BigInteger CreatedAt { get; init; }
    public BigInteger FillCount { get; init; }

    public PlaceProfiles Profiles { get; init; } = null!;
    public PlaceSecurity Security { get; init; } = null!;
    public PlaceChildren? Children { get; init; }
}