namespace Contracts.Application.Features.Marketing;

public sealed record GetPlaceAddressQuery(string MarketingAddr, int M, string? ParentAddr, int Pos) : IQuery<PlaceAddressResponse>;

internal sealed class GetPlaceAddressQueryHandler(IMarketingQueries queries)
    : IQueryHandler<GetPlaceAddressQuery, PlaceAddressResponse>
{
    public Task<Result<PlaceAddressResponse>> Handle(GetPlaceAddressQuery request, CancellationToken ct)
        => queries.GetPlaceAddrAsync(request.MarketingAddr, request.M, request.ParentAddr, request.Pos, ct);
}