namespace Matrix.Application.Features.Matrix;

public sealed record GetNextPosQuery(short M, string ProfileAddr): IQuery<NextPosResponse>;

internal sealed class GetNextPosQueryHandler(IFindNextPosService findNextPosService) : 
    IQueryHandler<GetNextPosQuery, NextPosResponse>
{
    public async Task<Result<NextPosResponse>> Handle(GetNextPosQuery request, CancellationToken cancellationToken)
    {
        var result = await findNextPosService.Find(request.M, request.ProfileAddr, cancellationToken);
        
        if (result.IsNotFound())
        {
            return Result<NextPosResponse>.NotFound();
        }

        if (!result.IsSuccess)
        {
            return Result<NextPosResponse>.Error(new ErrorList(result.Errors));
        }

        return Result.Success(new NextPosResponse
        {
            ParentAddr = result.Value.Addr,
            Pos = result.Value.Filling
        });
    }
}
