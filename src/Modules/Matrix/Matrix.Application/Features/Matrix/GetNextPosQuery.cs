namespace Matrix.Application.Features.Matrix;

public sealed record GetNextPosQuery(int M, string ProfileAddr): IQuery<NextPosResponse>;

internal sealed class GetNextPosQueryHandler : IQueryHandler<GetNextPosQuery, NextPosResponse>
{
    public Task<Result<NextPosResponse>> Handle(GetNextPosQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}