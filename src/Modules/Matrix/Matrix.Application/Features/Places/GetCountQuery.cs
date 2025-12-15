namespace Matrix.Application.Features.Places;

public sealed record GetCountQuery(int M, string ProfileAddr): IRequest<int>;

internal sealed class GetCountQueryHandler : IRequestHandler<GetCountQuery, int>
{
    public Task<int> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
