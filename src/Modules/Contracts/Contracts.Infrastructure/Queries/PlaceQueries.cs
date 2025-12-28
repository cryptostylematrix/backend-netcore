using static Contracts.Infrastructure.Caching.CacheEntryOptions;

namespace Contracts.Infrastructure.Queries;

public sealed class PlaceQueries(
    ITonClient tonClient,
    IDistributedCache cache,
    IOptions<TonQueryCacheOptions> cacheOpts) : IPlaceQueries
{
    private readonly TonQueryCacheOptions _cacheOpts = cacheOpts.Value;

    public Task<Result<PlaceDataResponse>> GetPlaceDataAsync(string addr, CancellationToken ct = default)
    {
        // normalize to avoid duplicate keys for different address string formats
        var normalizedAddr = new Address(addr).ToString();

        var key = $"{_cacheOpts.KeyPrefix}:place:data:{normalizedAddr}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache: cache,
            key: key,
            fetch: _ => FetchPlaceDataAsync(normalizedAddr),
            shouldCache: dto => dto.FillCount == 4,
            options: TtlDays(_cacheOpts.PlaceDataFilledTtlDays),
            ct: ct);
    }

    private async Task<Result<PlaceDataResponse>> FetchPlaceDataAsync(string addr)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_place_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<PlaceDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<PlaceDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // parent (stack[2]) optional cell -> optional address
            var parentCell = result.Value.Stack.TryGetClass<Cell>(2);
            var parentAddr = parentCell?.Parse().ReadAddress()?.ToString();

            // children (stack[7]) optional cell -> optional children dto
            var childrenCell = result.Value.Stack.TryGetClass<Cell>(7);
            var children = PlaceChildrenFromCell(childrenCell);

            // profiles/security are required cells (per your domain)
            var profilesCell = result.Value.Stack.TryGetClass<Cell>(5);
            var securityCell = result.Value.Stack.TryGetClass<Cell>(6);

            if (profilesCell is null || securityCell is null)
                return Result<PlaceDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var response = new PlaceDataResponse
            {
                MarketingAddr = ((Cell)result.Value.Stack[0]).Parse().ReadAddress()!.ToString(),
                M = (uint)(BigInteger)result.Value.Stack[1],
                ParentAddr = parentAddr,
                CreatedAt = (ulong)(BigInteger)result.Value.Stack[3],
                FillCount = (uint)(BigInteger)result.Value.Stack[4],
                Profiles = PlaceProfilesFromCell(profilesCell),
                Security = PlaceSecurityFromCell(securityCell),
                Children = children
            };

            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<PlaceDataResponse>.Error(exc.Message);
        }
    }

    private static PlaceProfilesResponse PlaceProfilesFromCell(Cell cell)
    {
        var slice = cell.Parse();

        var clone = (uint)slice.LoadUInt(1);
        var profile = slice.LoadAddress()!.ToString();
        var placeNumber = (uint)slice.LoadUInt(32);

        // Your domain code uses slice.LoadAddress() for inviter without presence bit;
        // it may return null if "addr_none". Keep it nullable.
        var inviter = slice.LoadAddress()?.ToString();

        return new PlaceProfilesResponse
        {
            Clone = clone,
            ProfileAddr = profile,
            PlaceNumber = placeNumber,
            InviterProfileAddr = inviter
        };
    }

    private static PlaceSecurityResponse PlaceSecurityFromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new PlaceSecurityResponse
        {
            AdminAddr = slice.LoadAddress()!.ToString()
        };
    }

    private static PlaceChildrenResponse? PlaceChildrenFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();
        var left = slice.LoadAddress()!.ToString();

        string? right = null;
        if ((uint)slice.LoadUInt(1) == 1)
            right = slice.LoadAddress()?.ToString();

        return new PlaceChildrenResponse
        {
            LeftAddr = left,
            RightAddr = right
        };
    }
}
