namespace ReferalProgram.Presentation.Endpoints.Inviters.GetInviter;

public sealed class GetInviterRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
