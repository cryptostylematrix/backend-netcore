using UI.Application.Abstractions;

namespace UI.Application.Features.Reports;

public sealed record GetProfilesReportQuery(int Page = 1) : IQuery<UiReportSectionResponse<ProfileReport>>;

internal sealed class GetProfilesReportQueryHandler(IUiReportQueries queries, TimeProvider clock)
    : IQueryHandler<GetProfilesReportQuery, UiReportSectionResponse<ProfileReport>>
{
    public async Task<Result<UiReportSectionResponse<ProfileReport>>> Handle(GetProfilesReportQuery request, CancellationToken ct)
    {
        if (request.Page < 1 || request.Page > 1000000)
            return Result<UiReportSectionResponse<ProfileReport>>.Error("invalid_page");
        var now = clock.GetUtcNow().UtcDateTime;
        var data = await queries.GetProfilesAsync(new(request.Page, 1, now, now, false, false, false, false), ct);
        return Result.Success(new UiReportSectionResponse<ProfileReport> { GeneratedAt = now, Data = data });
    }
}

public sealed record GetTonConnectReportQuery(bool GroupContract = true, bool GroupWalletName = false, bool GroupAppVersion = false, bool GroupPlatform = false) : IQuery<UiReportSectionResponse<ConnectionReport>>;

internal sealed class GetTonConnectReportQueryHandler(IUiReportQueries queries, TimeProvider clock)
    : IQueryHandler<GetTonConnectReportQuery, UiReportSectionResponse<ConnectionReport>>
{
    public async Task<Result<UiReportSectionResponse<ConnectionReport>>> Handle(GetTonConnectReportQuery request, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var data = await queries.GetTonConnectAsync(new(1, 1, now, now, request.GroupContract, request.GroupWalletName, request.GroupAppVersion, request.GroupPlatform), ct);
        return Result.Success(new UiReportSectionResponse<ConnectionReport> { GeneratedAt = now, Data = data });
    }
}

public sealed record GetActivityReportQuery(int Page = 1, string Period = "week") : IQuery<UiReportSectionResponse<ActivityReport>>;

internal sealed class GetActivityReportQueryHandler(IUiReportQueries queries, TimeProvider clock)
    : IQueryHandler<GetActivityReportQuery, UiReportSectionResponse<ActivityReport>>
{
    public async Task<Result<UiReportSectionResponse<ActivityReport>>> Handle(GetActivityReportQuery request, CancellationToken ct)
    {
        if (request.Page < 1 || request.Page > 1000000)
            return Result<UiReportSectionResponse<ActivityReport>>.Error("invalid_page");
        var now = clock.GetUtcNow().UtcDateTime;
        var from = ReportPeriods.From(request.Period, now);
        if (from is null) return Result<UiReportSectionResponse<ActivityReport>>.Error("invalid_period");
        var data = await queries.GetActivityAsync(new(1, request.Page, from.Value, now, false, false, false, false), ct);
        return Result.Success(new UiReportSectionResponse<ActivityReport> { GeneratedAt = now, Data = data });
    }
}

public sealed record GetPreferencesReportQuery() : IQuery<UiReportSectionResponse<PreferenceReport>>;

internal sealed class GetPreferencesReportQueryHandler(IUiReportQueries queries, TimeProvider clock)
    : IQueryHandler<GetPreferencesReportQuery, UiReportSectionResponse<PreferenceReport>>
{
    public async Task<Result<UiReportSectionResponse<PreferenceReport>>> Handle(GetPreferencesReportQuery request, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var data = await queries.GetPreferencesAsync(new(1, 1, now, now, false, false, false, false), ct);
        return Result.Success(new UiReportSectionResponse<PreferenceReport> { GeneratedAt = now, Data = data });
    }
}

