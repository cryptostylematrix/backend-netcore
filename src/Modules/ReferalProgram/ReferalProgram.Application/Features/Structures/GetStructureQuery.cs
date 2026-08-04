namespace ReferalProgram.Application.Features.Structures;

public sealed record GetStructureQuery(
    string MarketingAddr,
    byte StructureNumber) : IQuery<StructureResponse>;

internal sealed class GetStructureQueryHandler(IStructureQueries structureQueries)
    : IQueryHandler<GetStructureQuery, StructureResponse>
{
    public async Task<Result<StructureResponse>> Handle(
        GetStructureQuery request,
        CancellationToken cancellationToken)
    {
        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);

        return structure is null
            ? Result<StructureResponse>.NotFound()
            : Result.Success(structure);
    }
}
