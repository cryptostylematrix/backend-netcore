namespace Contracts.Application.Features.ProfileCollection;

public sealed record GetCollectionDataQuery() : IQuery<CollectionDataResponse>;

internal sealed class GetMultiDataQueryHandler(IProfileCollectionQueries queries)
    : IQueryHandler<GetCollectionDataQuery, CollectionDataResponse>
{
    public Task<Result<CollectionDataResponse>> Handle(GetCollectionDataQuery request, CancellationToken ct)
        => queries.GetCollectionDataAsync(ct);
}