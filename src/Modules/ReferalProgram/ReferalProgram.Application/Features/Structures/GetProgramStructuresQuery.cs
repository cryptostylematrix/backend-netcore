namespace ReferalProgram.Application.Features.Structures;

public sealed record GetProgramStructuresQuery(string MarketingAddr) : IQuery<IReadOnlyList<StructureResponse>>;

internal sealed class GetProgramStructuresQueryHandler(
    IProgramStructureListQueries structures,
    IReferalProgramQueries programs, ITonAddressComparer addresses)
    : IQueryHandler<GetProgramStructuresQuery, IReadOnlyList<StructureResponse>>
{
    public async Task<Result<IReadOnlyList<StructureResponse>>> Handle(GetProgramStructuresQuery request, CancellationToken ct)
    {
        var registered = await programs.GetAllAsync(ct);
        var address = registered.FirstOrDefault(program =>
            addresses.AreEqual(program.MarketingAddr, request.MarketingAddr))?.MarketingAddr;
        if (address is null) return Result<IReadOnlyList<StructureResponse>>.NotFound();
        var rows = await structures.GetAsync(address, ct);
        if (rows.Count == 0) return Result<IReadOnlyList<StructureResponse>>.NotFound();
        return Result.Success(rows);
    }
}
