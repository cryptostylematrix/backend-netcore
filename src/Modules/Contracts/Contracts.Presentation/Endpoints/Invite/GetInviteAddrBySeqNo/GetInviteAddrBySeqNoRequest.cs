namespace Contracts.Presentation.Endpoints.Invite.GetInviteAddrBySeqNo;

public sealed class GetInviteAddrBySeqNoRequest
{
    public string Addr { get; init; } = null!;
    public uint SeqNo { get; init; }
}