namespace Contracts.Presentation.Endpoints.ProfileItem.BuildMultiChooseInviterBody;

public sealed class BuildMultiChooseInviterBodyRequest
{
    public string ProfileAddr { get; init; } = null!;
    public string InviterAddr { get; init; } = null!;
    public int SeqNo { get; init; }
    public string InviteAddr { get; init; } = null!;
}