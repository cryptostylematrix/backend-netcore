using UI.Application.Abstractions;
using UI.Application.Features.TonConnections;
using UI.Core.TonConnectionAggregate;
using Xunit;

namespace UI.Application.Tests;

public sealed class TonConnectionHandlerTests
{
    [Fact]
    public async Task Saves_normalized_address_and_trimmed_metadata_then_forwards_changes()
    {
        var repository = new Repository();
        var handler = Handler(repository);
        var first = await handler.Handle(new(" wallet ", "boc", " Tonkeeper ", " 1.0 ", " android "), default);
        Assert.True(first.Value.Success);
        Assert.Equal("canonical", repository.Last!.WalletAddr);
        Assert.Equal(repository.Last.CreatedAtUtc, repository.Last.LastConnectedAtUtc);
        Assert.Equal(DateTimeKind.Utc, repository.Last.LastConnectedAtUtc!.Value.Kind);
        Assert.Equal("v5r1", repository.Last.ContractVersion);
        Assert.Equal("Tonkeeper", repository.Last.WalletName);
        Assert.Equal("1.0", repository.Last.AppVersion);
        Assert.Equal("android", repository.Last.Platform);
        var second = await handler.Handle(new("wallet", "boc", "Other wallet", "2.0", "browser"), default);
        Assert.True(second.Value.Success);
        Assert.Equal("Other wallet", repository.Last.WalletName);
        Assert.Equal("2.0", repository.Last.AppVersion);
        Assert.Equal("browser", repository.Last.Platform);
    }

    [Theory]
    [InlineData(null, "1.0", "android")]
    [InlineData("wallet", "", "android")]
    [InlineData("wallet", "1.0", "\nandroid")]
    public async Task Invalid_metadata_does_not_write(string? name, string version, string platform)
    {
        var repository = new Repository();
        var result = await Handler(repository).Handle(new("wallet", "boc", name, version, platform), default);
        Assert.False(result.Value.Success);
        Assert.Null(repository.Last);
    }

    [Fact]
    public async Task Rejects_wrong_state_init_without_overwriting_existing_metadata()
    {
        var repository = new Repository();
        var result = await Handler(repository).Handle(new("wallet", "invalid", "Wallet", "1", "browser"), default);
        Assert.False(result.Value.Success);
        Assert.Contains("err_invalid_wallet_state_init", result.Value.Errors);
        Assert.Null(repository.Last);
    }

    [Fact]
    public async Task Rejects_oversized_input_before_parsing()
    {
        var repository = new Repository();
        var result = await Handler(repository).Handle(new("wallet", new string('a', 65537), "Wallet", "1", "browser"), default);
        Assert.False(result.Value.Success);
        Assert.Null(repository.Last);
    }

    private static SaveTonConnectionCommandHandler Handler(Repository repository) =>
        new(new Addresses(), new Versions(), repository, TimeProvider.System);

    private sealed class Repository : ITonConnectionRepository
    {
        public TonConnection? Last { get; private set; }
        public Task UpsertAsync(TonConnection connection, CancellationToken cancellationToken)
        {
            Last = connection;
            return Task.CompletedTask;
        }
    }

    private sealed class Versions : IWalletContractVersionResolver
    {
        public bool TryResolve(string walletAddr, string? stateInit, out string version)
        {
            version = "v5r1";
            return stateInit == "boc";
        }
    }

    private sealed class Addresses : IWalletAddressService
    {
        public bool TryNormalize(string? address, out string normalizedAddress)
        {
            normalizedAddress = "canonical";
            return address?.Trim() == "wallet";
        }
        public bool AreEqual(string? left, string? right) => left == right;
        public IReadOnlyCollection<string> GetEquivalentRepresentations(string normalizedAddress) => [normalizedAddress];
    }
}
