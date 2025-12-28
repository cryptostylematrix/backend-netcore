namespace Contracts.Application.Features.Place;

public sealed record GetPlaceDataQuery(string Addr) : IQuery<PlaceDataResponse>;

internal sealed class GetPlaceDataQueryHandler(IPlaceQueries queries)
    : IQueryHandler<GetPlaceDataQuery, PlaceDataResponse>
{
    public Task<Result<PlaceDataResponse>> Handle(GetPlaceDataQuery request, CancellationToken ct)
        => queries.GetPlaceDataAsync(request.Addr, ct);
}