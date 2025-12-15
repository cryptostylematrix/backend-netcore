namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ProgramData
{
    public Address Inviter { get; init; } = null!;
    public BigInteger SeqNo { get; init; }
    public Address Invite { get; init; } = null!;
    public BigInteger Confirmed { get; init; }

    public static Cell Serialize(ProgramData data)
    {
        return new CellBuilder()
            .StoreAddress(data.Inviter)
            .StoreUInt(data.SeqNo, 32)
            .StoreAddress(data.Invite)
            .StoreUInt(data.Confirmed, 1)
            .Build();
    }
    
    public static ProgramData Deserialize(Cell cell)
    {
        var slice = cell.Parse();
        return new ProgramData
        {
            Inviter = slice.LoadAddress()!,
            SeqNo = slice.LoadUInt(32),
            Invite = slice.LoadAddress()!,
            Confirmed = slice.LoadUInt(1)
        };
    }
}