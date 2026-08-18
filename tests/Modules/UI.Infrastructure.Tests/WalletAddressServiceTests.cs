using TonSdk.Core;
using UI.Infrastructure.Services;
using Xunit;

namespace UI.Infrastructure.Tests;

public sealed class WalletAddressServiceTests
{
    private static readonly byte[] Hash =
        Enumerable.Repeat((byte)0xff, 32).ToArray();

    public static TheoryData<string> SupportedRepresentations()
    {
        var data = new TheoryData<string>();
        foreach (var representation in Representations())
            data.Add(representation);
        return data;
    }

    [Theory]
    [MemberData(nameof(SupportedRepresentations))]
    public void Normalizes_every_supported_representation_to_user_friendly_non_bounceable_address(
        string input)
    {
        var service = new WalletAddressService();

        var success = service.TryNormalize(input, out var normalized);

        Assert.True(success);
        Assert.Equal(NonBounceableAddress(), normalized);
    }

    [Fact]
    public void Treats_bounceable_and_non_bounceable_forms_as_equal()
    {
        var address = new Address(0, Hash);
        var bounceable = address.ToString(AddressType.Base64,
            new AddressStringifyOptions(true, false, true));
        var nonBounceable = address.ToString(AddressType.Base64,
            new AddressStringifyOptions(false, false, true));

        Assert.True(new WalletAddressService().AreEqual(
            bounceable,
            nonBounceable));
    }

    [Fact]
    public void Produces_all_equivalent_forms_for_legacy_row_lookup()
    {
        var service = new WalletAddressService();
        var representations = Representations();
        Assert.True(service.TryNormalize(representations[0], out var normalized));

        var equivalents = service.GetEquivalentRepresentations(normalized);

        Assert.All(representations, value => Assert.Contains(value, equivalents));
    }

    private static string[] Representations()
    {
        var address = new Address(0, Hash);
        return
        [
            address.ToString(AddressType.Raw),
            address.ToString(AddressType.Base64,
                new AddressStringifyOptions(
                    bounceable: true,
                    testOnly: false,
                    urlSafe: true)),
            address.ToString(AddressType.Base64,
                new AddressStringifyOptions(
                    bounceable: false,
                    testOnly: false,
                    urlSafe: true)),
            address.ToString(AddressType.Base64,
                new AddressStringifyOptions(
                    bounceable: true,
                    testOnly: false,
                    urlSafe: false))
        ];
    }

    private static string NonBounceableAddress()
    {
        var address = new Address(0, Hash);
        return address.ToString(
            AddressType.Base64,
            new AddressStringifyOptions(
                bounceable: false,
                testOnly: false,
                urlSafe: true,
                workchain: address.GetWorkchain()));
    }
}
