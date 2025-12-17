namespace Matrix.Application.Features.Places;

public record GetRootPlaceQuery(int M, string ProfileAddr): IQuery<PlaceResponse>;

internal sealed class GetRootPlaceQueryHandler : IQueryHandler<GetRootPlaceQuery, PlaceResponse>
{
    public Task<Result<PlaceResponse>> Handle(GetRootPlaceQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}