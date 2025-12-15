namespace Contracts.Domain.Aggregates.Place;

public sealed class PlaceProfiles
{
    public BigInteger Clone { get; init; }
    public Address Profile { get; init; } = null!;
    public BigInteger PlaceNumber { get; init; }
    public Address? InviterProfile { get; init; }

    public static PlaceProfiles FromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new PlaceProfiles
        {
            Clone = slice.LoadUInt(1),
            Profile = slice.LoadAddress()!,
            PlaceNumber = slice.LoadUInt(32),
            InviterProfile = slice.LoadAddress()
        };
    }
}