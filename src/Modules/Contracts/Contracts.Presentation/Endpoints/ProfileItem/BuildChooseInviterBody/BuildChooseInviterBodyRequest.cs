namespace Contracts.Presentation.Endpoints.ProfileItem.BuildChooseInviterBody;

public sealed class BuildChooseInviterBodyRequest
{
    public uint Program { get; init; }
    public string InviterAddr { get; init; } = null!;
    public int SeqNo { get; init; }
    public string InviteAddr { get; init; } = null!;
}