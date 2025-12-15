namespace Matrix.Application.Features.Places;

public record GetRootPlaceQuery(int M, string ProfileAddr): IRequest<PlaceResponse>;

internal sealed class GetRootPlaceQueryHandler : IRequestHandler<GetRootPlaceQuery, PlaceResponse>
{
    public Task<PlaceResponse> Handle(GetRootPlaceQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}