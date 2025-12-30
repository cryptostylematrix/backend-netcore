namespace Contracts.Application.Features.ProfileItem;

public sealed record BuildMultiChooseInviterBodyQuery(
    string ProfileAddr,
    string InviterAddr,
    int SeqNo,
    string InviteAddr) : IQuery<MultiChooseInviterBodyResponse>;

internal sealed class BuildMultiChooseInviterBodyQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<BuildMultiChooseInviterBodyQuery, MultiChooseInviterBodyResponse>
{
    public Task<Result<MultiChooseInviterBodyResponse>> Handle(BuildMultiChooseInviterBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildChooseInviterBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            profileAddr: request.ProfileAddr,
            program: 0x1ce8c484, // Multi program (485016708)
            inviterAddr: request.InviterAddr,
            seqNo: request.SeqNo,
            inviteAddr: request.InviteAddr));
}