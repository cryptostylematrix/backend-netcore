namespace ReferalProgram.Application.Features.Statistics;

public sealed record GetProgramStatisticsQuery(
    string MarketingAddr,
    string ProfileAddr) : IQuery<ProgramStatisticsResponse>;

internal sealed class GetProgramStatisticsQueryHandler(
    IProgramStatisticsQueries statisticsQueries)
    : IQueryHandler<GetProgramStatisticsQuery, ProgramStatisticsResponse>
{
    public async Task<Result<ProgramStatisticsResponse>> Handle(
        GetProgramStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MarketingAddr))
            return Result<ProgramStatisticsResponse>.Error("MarketingAddr is required.");

        if (string.IsNullOrWhiteSpace(request.ProfileAddr))
            return Result<ProgramStatisticsResponse>.Error("ProfileAddr is required.");

        var statistics = await statisticsQueries.GetAsync(
            request.MarketingAddr.Trim(),
            request.ProfileAddr.Trim(),
            cancellationToken);

        return statistics is null
            ? Result<ProgramStatisticsResponse>.NotFound()
            : Result.Success(statistics);
    }
}
