namespace Matrix.Application.Features.Places;

public sealed record GetNextPosQuery(int M, string ProfileAddr): IRequest<PlaceResponse>;

internal sealed class GetNextPosQueryHandler : IRequestHandler<GetNextPosQuery, PlaceResponse>
{
    public Task<PlaceResponse> Handle(GetNextPosQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}