namespace Contracts.Presentation.Endpoints.ProfileItem.BuildMultiChooseInviterBody;

public sealed class BuildMultiChooseInviterBodyRequest
{
    public string InviterAddr { get; init; } = null!;
    public int SeqNo { get; init; }
    public string InviteAddr { get; init; } = null!;
}