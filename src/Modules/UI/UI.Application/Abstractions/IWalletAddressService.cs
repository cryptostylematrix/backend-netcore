namespace UI.Application.Abstractions;

public interface IWalletAddressService
{
    bool TryNormalize(string? address, out string normalizedAddress);
    bool AreEqual(string? left, string? right);
    IReadOnlyCollection<string> GetEquivalentRepresentations(
        string normalizedAddress);
}
