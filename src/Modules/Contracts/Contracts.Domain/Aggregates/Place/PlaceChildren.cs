namespace Contracts.Domain.Aggregates.Place;

public sealed class PlaceChildren
{
    public Address Left { get; init; } = null!;
    public Address? Right { get; init; }

    public static PlaceChildren? FromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();
        var left = slice.LoadAddress()!;

        Address? right = null;
        if ((uint)slice.LoadUInt(1) == 1)
        {
            right = slice.LoadAddress();
        }

        return new PlaceChildren
        {
            Left = left,
            Right = right
        };
    }
}