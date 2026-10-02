using Contracts.Infrastructure.Metadata;

namespace Contracts.Infrastructure.Queries;

internal sealed class JettonMetadataQueries(
    IJettonWalletQueries wallets,
    IJetttonMinterQueries minters,
    IJettonMetadataDocumentClient documents,
    JettonMetadataCache cache) : IJettonMetadataQueries
{
    public async Task<Result<JettonMetadataResponse>> GetByWalletAsync(string address, CancellationToken ct)
    {
        string walletKey;
        try { walletKey = new Address(address.Trim()).ToString(AddressType.Raw); }
        catch { return Result<JettonMetadataResponse>.Invalid(new ValidationError("Invalid Jetton wallet address.")); }

        try
        {
            // Cache only the wallet-to-minter mapping, never balances or supply.
            var minter = await cache.GetAsync("wallet:" + walletKey, async token =>
            {
                var wallet = await wallets.GetWalletDataAsync(walletKey, token);
                token.ThrowIfCancellationRequested();
                if (!wallet.IsSuccess) return Result<MinterReference>.Error("Jetton wallet metadata unavailable.");
                return Result.Success(new MinterReference(new Address(wallet.Value.MinterAddr).ToString(AddressType.Raw)));
            }, ct);
            if (!minter.IsSuccess) return Result<JettonMetadataResponse>.Error("Jetton wallet metadata unavailable.");

            return await cache.GetAsync("minter:" + minter.Value.Address, async token =>
            {
                var data = await minters.GetJettonDataAsync(minter.Value.Address, token);
                token.ThrowIfCancellationRequested();
                if (!data.IsSuccess) return Result<JettonMetadataResponse>.Error("Jetton metadata unavailable.");
                using var external = string.IsNullOrWhiteSpace(data.Value.MetadataUri)
                    ? null : await documents.GetAsync(data.Value.MetadataUri, token);
                return Result.Success(Merge(minter.Value.Address, data.Value, external?.RootElement));
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException or FormatException or InvalidOperationException)
        {
            // Do not cache failures or expose remote payloads/URLs to API consumers.
            return Result<JettonMetadataResponse>.Error("Jetton metadata unavailable.");
        }
    }

    internal static JettonMetadataResponse Merge(string minter, JettonMinterDataResponse chain, JsonElement? external)
    {
        if (external is { ValueKind: not JsonValueKind.Object }) throw new JsonException("Expected metadata object.");
        byte decimals = chain.Decimals ?? 9;
        if (chain.Decimals is null && external is { } json && json.TryGetProperty("decimals", out var value))
        {
            var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
            if (!byte.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out decimals))
                throw new FormatException("Invalid metadata decimals.");
        }
        return new()
        {
            MinterAddr = minter,
            Name = Clean(chain.Name) ?? Text(external, "name"),
            Symbol = Clean(chain.Symbol) ?? Text(external, "symbol"),
            Decimals = decimals
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Text(JsonElement? json, string key) =>
        json is { } value && value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String ? Clean(item.GetString()) : null;

    private sealed record MinterReference(string Address);
}
