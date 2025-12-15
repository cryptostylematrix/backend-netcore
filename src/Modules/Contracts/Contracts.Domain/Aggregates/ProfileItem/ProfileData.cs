namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ProfileData
{
    public BigInteger IsInit { get; init; }
    public BigInteger Index { get; init; }
    public Address Collection { get; init; } = null!;
    public Address? Owner { get; init; }
    public ProfileContent? Content { get; init; }
}