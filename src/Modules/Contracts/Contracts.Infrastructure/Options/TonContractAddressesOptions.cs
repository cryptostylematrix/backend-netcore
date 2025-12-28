namespace Contracts.Infrastructure.Options;

public sealed class TonContractAddressesOptions
{
    public string MultiAddr { get; init; } = null!;
    public string ProfileCollectionAddr { get; init; } = null!;
}