namespace Contracts.Application.Features.Invite;

public sealed record GetInviteAddrBySeqNoQuery(string Addr, uint SeqNo) : IQuery<InviteAddressResponse>;

internal sealed class GetInviteAddressBySeqNoQueryHandler(IInviteQueries inviteQueries) :
    IQueryHandler<GetInviteAddrBySeqNoQuery, InviteAddressResponse>
{
    public Task<Result<InviteAddressResponse>> Handle(GetInviteAddrBySeqNoQuery request, CancellationToken ct)
        => inviteQueries.GetInviteAddressBySeqNoAsync(request.Addr, request.SeqNo, ct);
}