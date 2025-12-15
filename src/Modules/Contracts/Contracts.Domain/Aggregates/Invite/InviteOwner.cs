namespace Contracts.Domain.Aggregates.Invite;

public sealed class InviteOwner
{
    public Address Owner { get; init; } = null!;
    public BigInteger SetAt { get; init; }

    public static InviteOwner? FromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();

        return new InviteOwner
        {
            Owner = slice.LoadAddress()!,
            SetAt = slice.LoadUInt(64)
        };
    }

    private Cell ToCell()
    {
        return new CellBuilder()
            .StoreAddress(this.Owner)
            .StoreUInt(this.SetAt, 64)
            .Build();
    }

    public static Cell? ToCell(InviteOwner? data)
    {
        return data?.ToCell();
    }
}