
using static Contracts.Infrastructure.Caching.CacheEntryOptions;

namespace Contracts.Infrastructure.Queries;

public sealed class InviteQueries(
    ITonClient tonClient,
    IDistributedCache cache,
    IOptions<TonQueryCacheOptions> cacheOpts) : IInviteQueries
{
    private readonly TonQueryCacheOptions _cacheOpts = cacheOpts.Value;

    public Task<Result<InviteAddressResponse>> GetInviteAddressBySeqNoAsync(
        string addr, uint seqNo, CancellationToken ct = default)
    {
        var normalizedAddr = new Address(addr).ToString();
        var key = $"{_cacheOpts.KeyPrefix}:invite:addrBySeq:{normalizedAddr}:{seqNo}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache,
            key,
            fetch: _ => FetchInviteAddressBySeqNoAsync(normalizedAddr, seqNo), // your existing TON call logic
            shouldCache: _ => true,
            options: TtlDays(_cacheOpts.LongTtlDays),
            ct: ct);
    }

    private async Task<Result<InviteAddressResponse>> FetchInviteAddressBySeqNoAsync(
        string addr,
        uint seqNo)
    {
        try
        {
            var stackItems = new List<IStackItem>
            {
                new VmStackInt { Value = new BigInteger(seqNo) }
            };

            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_invite_address_by_seq_no",
                stackItems.ToArray());

            if (result is null)
                return Result<InviteAddressResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<InviteAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var inviteAddr = ((Cell)result.Value.Stack[0]).Parse().ReadAddress()?.ToString();
            if (string.IsNullOrWhiteSpace(inviteAddr))
                return Result<InviteAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            return Result.Success(new InviteAddressResponse { Addr = inviteAddr });
        }
        catch (Exception exc)
        {
            return Result<InviteAddressResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<InviteDataResponse>> GetInviteDataAsync(
        string addr,
        CancellationToken ct = default)
    {
        try
        {
            // keep same signature you used (args + extra null), if that's required by your SDK overload
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_invite_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<InviteDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<InviteDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // parent (stack[4]) is optional cell -> optional address
            var parentCell = result.Value.Stack.TryGetClass<Cell>(4);
            string? parentAddr = parentCell?.Parse().ReadAddress()?.ToString();

            // owner (stack[5]) is optional cell -> optional owner response
            var ownerCell = result.Value.Stack.TryGetClass<Cell>(5);

            var response = new InviteDataResponse
            {
                AdminAddr = ((Cell)result.Value.Stack[0]).Parse().ReadAddress()!.ToString(),
                Program = (int)(BigInteger)result.Value.Stack[1],
                NextRefNo = (int)(BigInteger)result.Value.Stack[2],
                Number = (int)(BigInteger)result.Value.Stack[3],
                ParentAddr = parentAddr,
                Owner = InviteOwnerFromCell(ownerCell),
            };

            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<InviteDataResponse>.Error(exc.Message);
        }
    }

    private static InviteOwnerResponse? InviteOwnerFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();

        return new InviteOwnerResponse
        {
            OwnerAddr = slice.LoadAddress()!.ToString(),
            SetAt = (long)slice.LoadUInt(64)
        };
    }
}
