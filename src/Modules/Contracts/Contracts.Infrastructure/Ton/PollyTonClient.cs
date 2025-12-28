using Polly;
using TonSdk.Core.Block;

namespace Contracts.Infrastructure.Ton;

public sealed class PollyTonClient(ITonClient inner, ResiliencePipeline pipeline) : ITonClient
{
    public Jetton Jetton => inner.Jetton;
    public Nft Nft => inner.Nft;
    public Wallet Wallet => inner.Wallet;
    public Dns Dns => inner.Dns;

    public TonClientType GetClientType() => inner.GetClientType();

    // ---- Exec helpers that compile with your ResiliencePipeline API ----

    private sealed class ResultBox<T>
    {
        public T? Value;
    }

    private async Task<T> Exec<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var box = new ResultBox<T>();

        // Uses: ExecuteAsync<TState>(Func<TState, CancellationToken, ValueTask> callback, TState state, CancellationToken ct)
        await pipeline.ExecuteAsync(
            static async (state, _) =>
            {
                // token is available (from Polly context), but TonSdk API doesn’t accept it.
                // Still useful for canceling wait-for-permit in the rate limiter.
                state.box.Value = await state.action().ConfigureAwait(false);
            },
            state: (action, box),
            cancellationToken: ct);

        return box.Value!;
    }

    // ---- ITonClient methods ----

    public Task<Coins> GetBalance(Address address, BlockIdExtended? block = null)
        => Exec(() => inner.GetBalance(address, block));

    public Task<bool> IsContractDeployed(Address address, BlockIdExtended? block = null)
        => Exec(() => inner.IsContractDeployed(address, block));

    public Task<AddressInformationResult?> GetAddressInformation(Address address, BlockIdExtended? block = null)
        => Exec(() => inner.GetAddressInformation(address, block));

    public Task<WalletInformationResult?> GetWalletInformation(Address address, BlockIdExtended? block = null)
        => Exec(() => inner.GetWalletInformation(address, block));

    public Task<MasterchainInformationResult?> GetMasterchainInfo()
        => Exec(inner.GetMasterchainInfo);

    public Task<BlockIdExtended> LookUpBlock(int workchain, long shard, long? seqno = null, ulong? lt = null, ulong? unixTime = null)
        => Exec(() => inner.LookUpBlock(workchain, shard, seqno, lt, unixTime));

    public Task<BlockDataResult?> GetBlockData(int workchain, long shard, long seqno, string? rootHash = null, string? fileHash = null)
        => Exec(() => inner.GetBlockData(workchain, shard, seqno, rootHash, fileHash));

    public Task<BlockTransactionsResult?> GetBlockTransactions(
        int workchain,
        long shard,
        long seqno,
        string? rootHash = null,
        string? fileHash = null,
        ulong? afterLt = null,
        string? afterHash = null,
        uint count = 10)
        => Exec(() => inner.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count));

    public Task<TransactionsInformationResult[]> GetTransactions(
        Address address,
        uint limit = 10,
        ulong? lt = null,
        string? hash = null,
        ulong? toLt = null,
        bool? archival = null)
        => Exec(() => inner.GetTransactions(address, limit, lt, hash, toLt, archival));

    public Task<RunGetMethodResult?> RunGetMethod(
        Address address,
        string method,
        IStackItem[] stackItems,
        BlockIdExtended? block = null)
        => Exec(() => inner.RunGetMethod(address, method, stackItems, block));

    public Task<RunGetMethodResult?> RunGetMethod(Address address, string method, string[][] stack)
        => Exec(() => inner.RunGetMethod(address, method, stack));

    public Task<SendBocResult?> SendBoc(Cell boc)
        => Exec(() => inner.SendBoc(boc));

    public Task<ConfigParamResult?> GetConfigParam(int configId, int? seqno = null)
        => Exec(() => inner.GetConfigParam(configId, seqno));

    public Task<ShardsInformationResult?> Shards(long seqno)
        => Exec(() => inner.Shards(seqno));

    public Task<IEstimateFeeResult> EstimateFee(MessageX message, bool ignoreChksig = true)
        => Exec(() => inner.EstimateFee(message, ignoreChksig));
}
