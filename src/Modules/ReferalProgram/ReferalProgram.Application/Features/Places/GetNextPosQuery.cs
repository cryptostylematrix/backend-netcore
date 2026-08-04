namespace ReferalProgram.Application.Features.Places;

public sealed record GetNextPosQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr) : IQuery<NextPosResponse>;

internal sealed class GetNextPosQueryHandler(INextPosService nextPosService)
    : IQueryHandler<GetNextPosQuery, NextPosResponse>
{
    public async Task<Result<NextPosResponse>> Handle(
        GetNextPosQuery request,
        CancellationToken ct)
    {
        var next = await nextPosService.GetNextPosAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            ct);

        return next is null
            ? Result<NextPosResponse>.NotFound()
            : Result.Success(next);
    }
}
