namespace ReferalProgram.Presentation.Endpoints.Invites.GetInviteInfo;

public sealed class GetInviteInfoRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
