namespace Contracts.Application.Features.ProfileItem;

public sealed record GetFreshNftDataQuery(string Addr) : IQuery<ProfileDataResponse>;

internal sealed class GetFreshNftDataQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<GetFreshNftDataQuery, ProfileDataResponse>
{
    public Task<Result<ProfileDataResponse>> Handle(
        GetFreshNftDataQuery request,
        CancellationToken ct)
        => queries.GetFreshNftDataAsync(request.Addr, ct);
}
