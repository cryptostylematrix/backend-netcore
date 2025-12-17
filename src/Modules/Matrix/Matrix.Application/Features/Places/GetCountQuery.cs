namespace Matrix.Application.Features.Places;

public sealed record GetCountQuery(int M, string ProfileAddr): IQuery<PlacesCountResponse>;

internal sealed class GetCountQueryHandler : IQueryHandler<GetCountQuery, PlacesCountResponse>
{
    public Task<Result<PlacesCountResponse>> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
