namespace Matrix.Application.Features.Matrix;

public sealed record GetNextPosQuery(short M, string ProfileAddr) : IQuery<NextPosResponse>;

internal sealed class GetNextPosQueryHandler(INextPosService nextPosService)
    : IQueryHandler<GetNextPosQuery, NextPosResponse>
{
    public async Task<Result<NextPosResponse>> Handle(GetNextPosQuery request, CancellationToken ct)
    {
        var next = await nextPosService.GetNextPosAsync(request.M, request.ProfileAddr, ct);
        return next is null
            ? Result<NextPosResponse>.NotFound()
            : Result.Success(next);
    }
}