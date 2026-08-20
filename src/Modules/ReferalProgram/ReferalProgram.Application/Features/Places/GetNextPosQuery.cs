namespace ReferalProgram.Application.Features.Places;

public sealed record GetNextPosQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    string? Operation) : IQuery<NextPosResponse>;

internal sealed class GetNextPosQueryHandler(INextPosService nextPosService)
    : IQueryHandler<GetNextPosQuery, NextPosResponse>
{
    public async Task<Result<NextPosResponse>> Handle(
        GetNextPosQuery request,
        CancellationToken ct)
    {
        PositionOperation? operation = null;
        if (!string.IsNullOrWhiteSpace(request.Operation))
        {
            if (!PositionOperationNames.TryParse(request.Operation, out var parsedOperation))
            {
                return Result<NextPosResponse>.Error(
                    $"Unknown position operation '{request.Operation}'. Supported operations: "
                    + string.Join(", ", PositionOperationNames.All) + ".");
            }

            operation = parsedOperation;
        }

        var next = await nextPosService.GetNextPosAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            operation,
            ct);

        return next is null
            ? Result<NextPosResponse>.NotFound()
            : Result.Success(next);
    }
}
