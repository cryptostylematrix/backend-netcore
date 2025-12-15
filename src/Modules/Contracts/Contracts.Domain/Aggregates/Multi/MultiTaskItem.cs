namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiTaskItem
{
    public BigInteger QueryId { get; init; }
    public BigInteger M { get; init; }
    public Address Profile { get; init; } = null!;
    public MultiTaskPayloadBase Payload { get; init; } = null!;
    

    public static Cell Serialize(MultiTaskItem item)
    {
        var builder = new CellBuilder();
        builder.StoreUInt(item.QueryId, 64);
        builder.StoreUInt(item.M, 3);
        builder.StoreAddress(item.Profile);

        var cell = item.Payload.ToCell(builder);
        return cell;
    }
    
    public static MultiTaskItem Deserialize(Cell cell)
    {
        var slice = cell.Parse();
        return new MultiTaskItem
        {
            QueryId = slice.LoadUInt(64),
            M = slice.LoadUInt(3),
            Profile = slice.LoadAddress()!,
            Payload = MultiTaskPayloadBase.FromSlice(slice)
        };
    }
    
    public static MultiTaskItem? FromCell(Cell? cell)
    {
        return cell is null ? null : Deserialize(cell);
    }
}

