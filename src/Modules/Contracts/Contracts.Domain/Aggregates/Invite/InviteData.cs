namespace Contracts.Domain.Aggregates.Invite;

public sealed class InviteData
{
    public Address Admin { get; init; } = null!;
    public BigInteger Program { get; init; }
    public BigInteger NextRefNo { get; init; }
    public BigInteger Number { get; init; }
    public Address? Parent { get; init; }
    public InviteOwner? Owner { get; init; }
}