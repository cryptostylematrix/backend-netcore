namespace Contracts.Application.Features.ProfileItem;

public sealed record GetProgramsQuery(string Addr) : IQuery<ProfileProgramsResponse>;

internal sealed class GetProgramsQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<GetProgramsQuery, ProfileProgramsResponse>
{
    public Task<Result<ProfileProgramsResponse>> Handle(GetProgramsQuery request, CancellationToken ct)
        => queries.GetProgramsAsync(request.Addr, ct);
}