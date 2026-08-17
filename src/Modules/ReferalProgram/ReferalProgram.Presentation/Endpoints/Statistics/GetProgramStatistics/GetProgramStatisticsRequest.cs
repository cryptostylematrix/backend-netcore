namespace ReferalProgram.Presentation.Endpoints.Statistics.GetProgramStatistics;

public sealed class GetProgramStatisticsRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
