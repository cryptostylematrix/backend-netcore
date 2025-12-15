namespace Contracts.Domain.Aggregates.Place;

public sealed class PlaceSecurity
{
    public Address Admin { get; init; } = null!;

    public static PlaceSecurity FromCell(Cell cell)
    {
        var slice = cell.Parse();

        return new PlaceSecurity
        {
            Admin = slice.LoadAddress()!
        };
    }
}