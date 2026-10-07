using UI.Application.Abstractions;

namespace UI.Application.Features.Reports;

public sealed record GetUiReportQuery(int ProfilePage, int ActivityPage,
    string Period, bool GroupContract, bool GroupWalletName, bool GroupAppVersion, bool GroupPlatform) : IQuery<UiReportResponse>;

internal sealed class GetUiReportQueryHandler(IUiReportQueries queries,
    TimeProvider timeProvider) : IQueryHandler<GetUiReportQuery, UiReportResponse>
{
    public async Task<Result<UiReportResponse>> Handle(GetUiReportQuery request, CancellationToken ct)
    {
        if (request.ProfilePage < 1 || request.ActivityPage < 1
            || request.ProfilePage > 1000000 || request.ActivityPage > 1000000)
            return Result<UiReportResponse>.Error("invalid_page");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var from = ReportPeriods.From(request.Period, now);
        if (from is null) return Result<UiReportResponse>.Error("invalid_period");
        return Result.Success(await queries.GetAsync(new(request.ProfilePage, request.ActivityPage,
            from.Value, now, request.GroupContract, request.GroupWalletName, request.GroupAppVersion, request.GroupPlatform), ct));
    }
}

public static class ReportPeriods
{
    public static DateTime? From(string period, DateTime now) => period switch
    {
        "hour" => now.AddHours(-1),
        "today" => now.Date,
        "week" => now.AddDays(-7),
        "month" => now.AddMonths(-1),
        "three_months" => now.AddMonths(-3),
        "six_months" => now.AddMonths(-6),
        "year" => now.AddYears(-1),
        _ => null
    };
}
