using TonSdk.Core;
using UI.Application.Abstractions;

namespace UI.Infrastructure.Services;

internal sealed class WalletAddressService : IWalletAddressService
{
    public bool TryNormalize(string? address, out string normalizedAddress)
    {
        normalizedAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        try
        {
            var parsedAddress = new Address(address.Trim());
            normalizedAddress = parsedAddress.ToString(
                AddressType.Base64,
                new AddressStringifyOptions(
                    bounceable: false,
                    testOnly: false,
                    urlSafe: true,
                    workchain: parsedAddress.GetWorkchain()));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool AreEqual(string? left, string? right)
    {
        if (!TryNormalize(left, out var normalizedLeft)
            || !TryNormalize(right, out var normalizedRight))
        {
            return false;
        }

        return string.Equals(
            normalizedLeft,
            normalizedRight,
            StringComparison.Ordinal);
    }

    public IReadOnlyCollection<string> GetEquivalentRepresentations(
        string normalizedAddress)
    {
        var address = new Address(normalizedAddress);
        var values = new HashSet<string>(StringComparer.Ordinal)
        {
            address.ToString(AddressType.Raw)
        };

        foreach (var bounceable in new[] { true, false })
        foreach (var testOnly in new[] { false, true })
        foreach (var urlSafe in new[] { true, false })
        {
            values.Add(address.ToString(
                AddressType.Base64,
                new AddressStringifyOptions(
                    bounceable,
                    testOnly,
                    urlSafe,
                    address.GetWorkchain())));
        }

        return values.ToArray();
    }
}
