namespace Contracts.Application.Abstractions;

public interface IProfileCollectionQueries
{
    Task<Result<NftAddressResponse>> GetNftAddressByLoginAsync(string login, CancellationToken ct = default);
    Task<Result<CollectionDataResponse>> GetCollectionDataAsync(CancellationToken ct = default);
    Result<DeployItemBodyResponse> BuildDeployItemBody(
        long queryId,
        string login, 
        string? imageUrl, 
        string? firstName, 
        string? lastName, 
        string? tgUsername);
}