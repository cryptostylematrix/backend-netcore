namespace Contracts.Application.Features.Multi;

public sealed record GetMultiDataQuery() : IQuery<MultiDataResponse>;

internal sealed class GetMultiDataQueryHandler(IMultiQueries queries)
    : IQueryHandler<GetMultiDataQuery, MultiDataResponse>
{
    public Task<Result<MultiDataResponse>> Handle(GetMultiDataQuery request, CancellationToken ct)
        => queries.GetMultiDataAsync(ct);
}