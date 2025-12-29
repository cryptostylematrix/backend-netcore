namespace Contracts.Application.Abstractions;

public interface IProfileCollectionQueries
{
    Task<Result<NftAddressResponse>> GetNftAddressByLoginAsync(string login, CancellationToken ct = default);
    Task<Result<CollectionDataResponse>> GetCollectionDataAsync(CancellationToken ct = default);
}