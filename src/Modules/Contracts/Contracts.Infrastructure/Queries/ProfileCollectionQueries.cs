using Contracts.Infrastructure.NftContent;
using Contracts.Infrastructure.Ton;
using static Contracts.Infrastructure.Caching.CacheEntryOptions;

namespace Contracts.Infrastructure.Queries;

public sealed class ProfileCollectionQueries(
    ITonClient tonClient,
    IDistributedCache cache,
    IOptions<TonQueryCacheOptions> cacheOpts,
    IOptions<TonContractAddressesOptions> options) : IProfileCollectionQueries
{
    private readonly TonQueryCacheOptions _cacheOpts = cacheOpts.Value;
    private readonly Address _profileCollectionAddress = new(options.Value.ProfileCollectionAddr);
    
    public Task<Result<NftAddressResponse>> GetNftAddressByLoginAsync(string login, CancellationToken ct = default)
    {
        var loginKey = login.Trim().ToLowerInvariant();
        var key = $"{_cacheOpts.KeyPrefix}:profileCollection:nftByLogin:{loginKey}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache,
            key,
            fetch: _ => FetchNftAddressByLoginAsync(login),
            shouldCache: _ => true,
            options: TtlDays(_cacheOpts.LongTtlDays),
            ct: ct);
    }


    private async Task<Result<NftAddressResponse>> FetchNftAddressByLoginAsync(string login)
    {
        try
        {
            var index = TonHashUtils.Sha256N(login);

            var stackItems = new IStackItem[]
            {
                new VmStackInt { Value = index }
            };

            var result = await tonClient.RunGetMethod(
                _profileCollectionAddress,
                "get_nft_address_by_index",
                stackItems);

            if (result is null)
                return Result<NftAddressResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<NftAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var cell = result.Value.Stack.TryGetClass<Cell>(0);
            if (cell is null)
                return Result<NftAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var addr = cell.Parse().ReadAddress()?.ToString();
            return string.IsNullOrWhiteSpace(addr) ?
                Result<NftAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed)) : 
                Result.Success(new NftAddressResponse { Addr = addr });
        }
        catch (Exception exc)
        {
            return Result<NftAddressResponse>.Error(exc.Message);
        }
    }
    
    public Task<Result<CollectionDataResponse>> GetCollectionDataAsync(CancellationToken ct = default)
    {
        var key = $"{_cacheOpts.KeyPrefix}:profileCollection:collectionData";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache,
            key,
            fetch: _ => FetchCollectionDataAsync(ct),
            shouldCache: _ => true,
            options: TtlDays(_cacheOpts.LongTtlDays),
            ct: ct);
    }

    public Result<DeployItemBodyResponse> BuildDeployItemBody(long queryId, string login, string? imageUrl, string? firstName, string? lastName,
        string? tgUsername)
    {
        try
        {
            var content = ProfileNftContent.ProfileToNftContent(
                login: login,
                imageUrl: imageUrl,
                firstName: firstName,
                lastName: lastName,
                tgUsername: tgUsername);
            
            var contentCell = NftContentWriter.NftContentToCell(content);
            
            var builder = new CellBuilder();
            builder.StoreUInt(1, 32); // COLLECTION_DEPLOY_ITEM
            builder.StoreUInt(queryId, 64);
            builder.StoreStringTail(login.ToLowerForProfile() ?? throw new ArgumentNullException(nameof(login)));
            builder.StoreRef(contentCell);
            
            return Result<DeployItemBodyResponse>.Success(new DeployItemBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<DeployItemBodyResponse>.Error(e.Message);
        }
    }


    private async Task<Result<CollectionDataResponse>> FetchCollectionDataAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                _profileCollectionAddress,
                "get_collection_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<CollectionDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<CollectionDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // Stack layout (from your MultiContract):
            // 0 nextItemIndex 
            // 1 collectionContent (cell)
            // 2 owner (cell->address)
         
            var owner = ((Cell)result.Value.Stack[2]).Parse().LoadAddress()!.ToString();

            return Result.Success(new CollectionDataResponse
            {
                Addr = _profileCollectionAddress.ToString(),
                OwnerAddr = owner
            });
        }
        catch (Exception exc)
        {
            return Result<CollectionDataResponse>.Error(exc.Message);
        }
    }
}
