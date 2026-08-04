namespace ReferalProgram.Presentation.Endpoints.Invites.GetRootInviteInfo;

public sealed class GetRootInviteInfoRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
}
