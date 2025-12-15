namespace Contracts.Domain.Aggregates.Multi;

public sealed class PlacePosData
{
    public Address Parent { get; private init; } = null!;
    public uint Pos { get; init; }

    public static Cell? ToCell(PlacePosData? data)
    {
        if (data is null) return null;

        return new CellBuilder()
            .StoreAddress(data.Parent)
            .StoreUInt(data.Pos, 1)
            .Build();
    }

    public static PlacePosData? FromCell(Cell? cell)
    {
        if (cell is null) return null;

        var s = cell.Parse();

        return new PlacePosData
        {
            Parent = s.LoadAddress()!,
            Pos = (uint)s.LoadUInt(1)
        };
    }
}