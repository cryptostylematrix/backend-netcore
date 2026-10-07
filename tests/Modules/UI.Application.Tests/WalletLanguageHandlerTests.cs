using UI.Application.Abstractions;
using UI.Application.Features.WalletPreferences;
using UI.Core.WalletPreferencesAggregate;
using UI.Dto;
using Xunit;

namespace UI.Application.Tests;

public sealed class WalletLanguageHandlerTests
{
    [Theory]
    [InlineData("de")][InlineData("en")][InlineData("es")][InlineData("fr")]
    [InlineData("hu")][InlineData("it")][InlineData("kk")][InlineData("pl")]
    [InlineData("pt")][InlineData("ru")][InlineData("uk")]
    [InlineData("ja")][InlineData("pt-br")][InlineData("zh-hant")][InlineData("sr-latn-rs")]
    public async Task Supports_current_and_future_language_tags(string language)
    {
        var repository = new Repository();
        var handler = new SaveWalletLanguageCommandHandler(new Addresses(), repository, TimeProvider.System);
        var response = await handler.Handle(new(" wallet ", $" {language.ToUpperInvariant()} ", true), default);
        Assert.True(response.Value.Success);
        Assert.Equal(language, response.Value.Language);
        Assert.Equal("canonical", repository.Last!.WalletAddr);
        Assert.True(repository.Overwrite);
    }

    [Fact]
    public async Task Resolve_returns_the_database_value_and_does_not_request_overwrite()
    {
        var repository = new Repository { Stored = "de" };
        var handler = new SaveWalletLanguageCommandHandler(new Addresses(), repository, TimeProvider.System);
        var response = await handler.Handle(new("wallet", "ru", false), default);
        Assert.Equal("de", response.Value.Language);
        Assert.False(repository.Overwrite);
    }

    [Theory]
    [InlineData(null)][InlineData("")][InlineData("../ru")][InlineData("ru_RU")][InlineData("r")][InlineData("en--us")]
    public async Task Rejects_unsupported_language_before_persistence(string? language)
    {
        var repository = new Repository();
        var handler = new SaveWalletLanguageCommandHandler(new Addresses(), repository, TimeProvider.System);
        var response = await handler.Handle(new("wallet", language, true), default);
        Assert.False(response.Value.Success);
        Assert.Contains(UiErrorCodes.InvalidLanguage, response.Value.Errors);
        Assert.Null(repository.Last);
    }

    [Fact]
    public async Task Rejects_invalid_wallet_before_persistence()
    {
        var repository = new Repository();
        var handler = new SaveWalletLanguageCommandHandler(new Addresses(), repository, TimeProvider.System);
        var response = await handler.Handle(new("invalid", "en", true), default);
        Assert.Contains(UiErrorCodes.InvalidWalletAddress, response.Value.Errors);
        Assert.Null(repository.Last);
    }

    private sealed class Repository : IWalletPreferencesRepository
    {
        public WalletPreferences? Last { get; private set; }
        public bool Overwrite { get; private set; }
        public string? Stored { get; init; }
        public Task<string> SaveLanguageAsync(WalletPreferences preferences, bool overwrite, CancellationToken ct)
        {
            Last = preferences;
            Overwrite = overwrite;
            return Task.FromResult(Stored ?? preferences.Language);
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
