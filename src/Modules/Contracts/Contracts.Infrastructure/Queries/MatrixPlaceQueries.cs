using static Contracts.Infrastructure.Caching.CacheEntryOptions;

namespace Contracts.Infrastructure.Queries;

public sealed class MatrixPlaceQueries(
    ITonClient tonClient,
    IDistributedCache cache,
    IOptions<TonQueryCacheOptions> cacheOpts) : IMatrixPlaceQueries
{
    private readonly TonQueryCacheOptions _cacheOpts = cacheOpts.Value;

    public Task<Result<MatrixPlaceDataResponse>> GetPlaceDataAsync(string addr, CancellationToken ct = default)
    {
        // normalize to avoid duplicate keys for different address string formats
        var normalizedAddr = new Address(addr).ToString();

        var key = $"{_cacheOpts.KeyPrefix}:matrix_place:data:{normalizedAddr}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache: cache,
            key: key,
            fetch: _ => FetchPlaceDataAsync(normalizedAddr),
            shouldCache: dto => (dto.Width > 0) && (dto.SeqNo == dto.Width),
            options: TtlDays(_cacheOpts.PlaceDataFilledTtlDays),
            ct: ct);
    }

    private async Task<Result<MatrixPlaceDataResponse>> FetchPlaceDataAsync(string addr)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_place_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MatrixPlaceDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MatrixPlaceDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // parent (stack[2]) optional cell -> optional address
            var parentCell = result.Value.Stack.TryGetClass<Cell>(3);
            var parentAddr = parentCell?.Parse().ReadAddress()?.ToString();
            
            // parent (stack[8]) optional cell -> optional address
            var adminAddressCell = result.Value.Stack.TryGetClass<Cell>(8);
            var adminAddress = adminAddressCell?.Parse().ReadAddress()?.ToString();
            
            // info (stack[9]) optional cell -> optional children dto
            var infoCell = result.Value.Stack.TryGetClass<Cell>(9);
            var info = PlaceInfoFromCell(infoCell);

            // descendants (stack[10]) optional cell -> optional children dto
            var descendantsCell = result.Value.Stack.TryGetClass<Cell>(10);
            var descendants = PlaceDescendantsFromCell(descendantsCell);
            
            
            var response = new MatrixPlaceDataResponse
            {
                Init = (int)(BigInteger)result.Value.Stack[0] != 0,
                MarketingAddr = ((Cell)result.Value.Stack[1]).Parse().ReadAddress()!.ToString(),
                M = (byte)(BigInteger)result.Value.Stack[2],
                ParentAddr = parentAddr,
                Pos = (uint)(BigInteger)result.Value.Stack[4],
                
                SeqNo = (uint)(BigInteger)result.Value.Stack[5],
                Width =  (byte)(BigInteger)result.Value.Stack[6],
                Height = (byte)(BigInteger)result.Value.Stack[7],
                
                AdminAddr = adminAddress,
                Info = info,
                Descendants = descendants,
            };

            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<MatrixPlaceDataResponse>.Error(exc.Message);
        }
    }

    private static PlaceInfoResponse? PlaceInfoFromCell(Cell? cell)
    {
        if (cell is null) return null;
        var slice = cell.Parse();

        var kind = (byte)slice.LoadUInt(4);
        var profile = slice.LoadAddress()!.ToString();
        var placeNumber = (uint)slice.LoadUInt(32);

        // Your domain code uses slice.LoadAddress() for inviter without presence bit;
        // it may return null if "addr_none". Keep it nullable.
        var inviter = slice.LoadAddress()?.ToString();

        return new PlaceInfoResponse
        {
            Kind = kind,
            ProfileAddr = profile,
            PlaceNumber = placeNumber,
            InviterProfileAddr = inviter
        };
    }
    
    private static PlaceDescendantsResponse? PlaceDescendantsFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();

        return new PlaceDescendantsResponse
        {
          
        };
    }
}