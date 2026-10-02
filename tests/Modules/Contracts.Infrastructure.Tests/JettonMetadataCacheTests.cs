using Ardalis.Result;
using Contracts.Application.Abstractions;
using Contracts.Dto;
using Contracts.Infrastructure.Caching;
using Contracts.Infrastructure.Metadata;
using Contracts.Infrastructure.Options;
using Contracts.Infrastructure.Queries;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TonSdk.Core;

namespace Contracts.Infrastructure.Tests;

public sealed class JettonMetadataCacheTests
{
    private static readonly string Wallet = "0:" + new string('a', 64);
    private static readonly string OtherWallet = "0:" + new string('b', 64);
    private static readonly string Minter = "0:" + new string('c', 64);

    [Fact]
    public async Task Different_wallets_share_the_same_minter_metadata_and_address_aliases_share_mapping()
    {
        var storage = new Storage();
        using var cache = new JettonMetadataCache(storage, Microsoft.Extensions.Options.Options.Create(new TonQueryCacheOptions()));
        var wallets = new Wallets();
        var minters = new Minters();
        var documents = new Documents();
        var query = new JettonMetadataQueries(wallets, minters, documents, cache);
        Assert.True((await query.GetByWalletAsync(Wallet, default)).IsSuccess);
        var friendly = new Address(Wallet).ToString(AddressType.Base64);
        Assert.True((await query.GetByWalletAsync(friendly, default)).IsSuccess);
        var result = await query.GetByWalletAsync(OtherWallet, default);
        Assert.Equal("Example token", result.Value.Name);
        Assert.Equal("EXM", result.Value.Symbol);
        Assert.Equal(6, result.Value.Decimals);
        Assert.Equal(2, wallets.Calls);
        Assert.Equal(1, minters.Calls);
        Assert.Equal(1, documents.Calls);
        Assert.Equal(TimeSpan.FromHours(24), storage.LastTtl);
    }

    [Fact]
    public async Task Concurrent_requests_are_coalesced_and_expired_metadata_is_refreshed()
    {
        var storage = new Storage();
        using var cache = new JettonMetadataCache(storage, Microsoft.Extensions.Options.Options.Create(new TonQueryCacheOptions { JettonMetadataTtlHours = 2 }));
        var wallets = new Wallets();
        var minters = new Minters();
        var documents = new Documents();
        var query = new JettonMetadataQueries(wallets, minters, documents, cache);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => query.GetByWalletAsync(Wallet, default)));
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, wallets.Calls);
        Assert.Equal(1, documents.Calls);
        storage.Now += TimeSpan.FromHours(3);
        Assert.True((await query.GetByWalletAsync(Wallet, default)).IsSuccess);
        Assert.Equal(2, documents.Calls);
    }

    [Fact]
    public async Task A_failed_metadata_download_is_not_cached_or_replaced_with_invented_decimals()
    {
        using var cache = new JettonMetadataCache(new Storage(), Microsoft.Extensions.Options.Options.Create(new TonQueryCacheOptions()));
        var documents = new Documents { Fail = true };
        var minters = new Minters();
        var query = new JettonMetadataQueries(new Wallets(), minters, documents, cache);
        Assert.False((await query.GetByWalletAsync(Wallet, default)).IsSuccess);
        documents.Fail = false;
        var success = await query.GetByWalletAsync(Wallet, default);
        Assert.True(success.IsSuccess);
        Assert.Equal(2, documents.Calls);
        Assert.Equal(6, success.Value.Decimals);
    }

    [Fact]
    public async Task Caller_cancellation_is_propagated()
    {
        using var cache = new JettonMetadataCache(new Storage(), Microsoft.Extensions.Options.Options.Create(new TonQueryCacheOptions()));
        var query = new JettonMetadataQueries(new Wallets(), new Minters(), new Documents(), cache);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => query.GetByWalletAsync(Wallet, cancellation.Token));
    }

    private sealed class Wallets : IJettonWalletQueries
    {
        public int Calls;
        public Task<Result<JettonWalletDataResponse>> GetWalletDataAsync(string addr, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(Result.Success(new JettonWalletDataResponse { MinterAddr = Minter }));
        }
        public Result<JettonTransferMsgBodyResponse> BuildTransferMsgBody(ulong queryId, ulong amount, string destinationAddr, string? responseDestinationAddr, string? customPayloadBocHex, ulong forwardTonAmount, string? forwardPayloadBocHex) => throw new NotSupportedException();
    }

    private sealed class Minters : IJetttonMinterQueries
    {
        public int Calls;
        public Task<Result<JettonMinterDataResponse>> GetJettonDataAsync(string addr, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(Result.Success(new JettonMinterDataResponse { MetadataUri = "https://example.com/token.json" }));
        }
        public Task<Result<JettonWalletAddressResponse>> GetWalletAddressAsync(string addr, string ownerAddr, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class Documents : IJettonMetadataDocumentClient
    {
        public bool Fail;
        public int Calls;
        public async Task<JsonDocument> GetAsync(string uri, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            if (Fail) throw new HttpRequestException("Temporary failure");
            return JsonDocument.Parse("""{"name":"Example token","symbol":"EXM","decimals":"6"}""");
        }
    }

    private sealed class Storage : IDistributedCache
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Value, DateTimeOffset Expires)> entries = new();
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public TimeSpan? LastTtl;
        public byte[]? Get(string key) => entries.TryGetValue(key, out var row) && row.Expires > Now ? row.Value : null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            LastTtl = options.AbsoluteExpirationRelativeToNow;
            entries[key] = (value, Now + LastTtl!.Value);
        }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => entries.TryRemove(key, out _);
        public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
    }
}
