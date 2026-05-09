namespace Contracts.Application.Features.ProfileItem;

public sealed record BuildChooseInviterBodyQuery(
    uint Program,
    string InviterAddr,
    int SeqNo,
    string InviteAddr) : IQuery<ChooseInviterBodyResponse>;

internal sealed class BuildChooseInviterBodyQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<BuildChooseInviterBodyQuery, ChooseInviterBodyResponse>
{
    public Task<Result<ChooseInviterBodyResponse>> Handle(BuildChooseInviterBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildChooseInviterBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            program: request.Program,
            inviterAddr: request.InviterAddr,
            seqNo: request.SeqNo,
            inviteAddr: request.InviteAddr));
}