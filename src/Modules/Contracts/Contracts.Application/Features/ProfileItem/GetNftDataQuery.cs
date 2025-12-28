namespace Contracts.Application.Features.ProfileItem;

public sealed record GetNftDataQuery(string Addr) : IQuery<ProfileDataResponse>;

internal sealed class GetNftDataQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<GetNftDataQuery, ProfileDataResponse>
{
    public Task<Result<ProfileDataResponse>> Handle(GetNftDataQuery request, CancellationToken ct)
        => queries.GetNftDataAsync(request.Addr, ct);
}