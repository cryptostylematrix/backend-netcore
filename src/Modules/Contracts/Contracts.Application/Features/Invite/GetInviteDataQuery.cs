namespace Contracts.Application.Features.Invite;

public sealed record GetInviteDataQuery(string Addr) : IQuery<InviteDataResponse>;

internal sealed class GetInviteDataQueryHandler(IInviteQueries inviteQueries)
    : IQueryHandler<GetInviteDataQuery, InviteDataResponse>
{
    public Task<Result<InviteDataResponse>> Handle(GetInviteDataQuery request, CancellationToken ct)
        => inviteQueries.GetInviteDataAsync(request.Addr, ct);
}