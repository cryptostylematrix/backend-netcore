namespace Contracts.Application.Features.MatrixPlace;

public sealed record GetMatrixPlaceDataQuery(string Addr) : IQuery<MatrixPlaceDataResponse>;

internal sealed class GetPlaceDataQueryHandler(IMatrixPlaceQueries queries)
    : IQueryHandler<GetMatrixPlaceDataQuery, MatrixPlaceDataResponse>
{
    public Task<Result<MatrixPlaceDataResponse>> Handle(GetMatrixPlaceDataQuery request, CancellationToken ct)
        => queries.GetPlaceDataAsync(request.Addr, ct);
}