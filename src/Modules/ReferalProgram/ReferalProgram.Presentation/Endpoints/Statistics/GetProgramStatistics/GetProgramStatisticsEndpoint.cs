using ReferalProgram.Application.Features.Statistics;

namespace ReferalProgram.Presentation.Endpoints.Statistics.GetProgramStatistics;

public sealed class GetProgramStatisticsEndpoint(ISender sender)
    : Endpoint<GetProgramStatisticsRequest, ProgramStatisticsResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/statistics");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get referral-program statistics";
            summary.Description =
                "Gets overall place totals and direct-referral participation "
                + "statistics for every configured structure.";
        });
    }

    public override async Task HandleAsync(
        GetProgramStatisticsRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProgramStatisticsQuery(
            request.MarketingAddr,
            request.ProfileAddr), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
