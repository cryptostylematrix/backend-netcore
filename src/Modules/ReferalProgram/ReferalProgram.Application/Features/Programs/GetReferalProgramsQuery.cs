namespace ReferalProgram.Application.Features.Programs;

public sealed record GetReferalProgramsQuery : IQuery<IReadOnlyCollection<ReferalProgramResponse>>;

internal sealed class GetReferalProgramsQueryHandler(IReferalProgramQueries referalProgramQueries)
    : IQueryHandler<GetReferalProgramsQuery, IReadOnlyCollection<ReferalProgramResponse>>
{
    public async Task<Result<IReadOnlyCollection<ReferalProgramResponse>>> Handle(
        GetReferalProgramsQuery request,
        CancellationToken cancellationToken)
    {
        var programs = await referalProgramQueries.GetAllAsync(cancellationToken);
        return Result.Success(programs);
    }
}
