using Microsoft.AspNetCore.Http;
using UI.Application.Features.Reports;

namespace UI.Presentation.Endpoints.Reports;

public sealed class ProfilesReportRequest
{
    [BindFrom("page")] public int Page { get; init; } = 1;
}
public sealed class ProfilesReportEndpoint(ISender sender) : Endpoint<ProfilesReportRequest, UiReportSectionResponse<ProfileReport>>
{
    public override void Configure()
    {
        Get("/api/ui/reports/profiles");
        AllowAnonymous();
        Tags("UI Reports");
    }
    public override async Task HandleAsync(ProfilesReportRequest request, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new GetProfilesReportQuery(request.Page), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(Results.StatusCode(400)); return; }
        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class TonConnectReportRequest
{
    [BindFrom("group_contract")] public bool GroupContract { get; init; } = true;
    [BindFrom("group_wallet_name")] public bool GroupWalletName { get; init; }
    [BindFrom("group_app_version")] public bool GroupAppVersion { get; init; }
    [BindFrom("group_platform")] public bool GroupPlatform { get; init; }
}
public sealed class TonConnectReportEndpoint(ISender sender) : Endpoint<TonConnectReportRequest, UiReportSectionResponse<ConnectionReport>>
{
    public override void Configure()
    {
        Get("/api/ui/reports/ton-connect");
        AllowAnonymous();
        Tags("UI Reports");
    }
    public override async Task HandleAsync(TonConnectReportRequest request, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new GetTonConnectReportQuery(request.GroupContract, request.GroupWalletName, request.GroupAppVersion, request.GroupPlatform), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(Results.StatusCode(400)); return; }
        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class ActivityReportRequest
{
    [BindFrom("page")] public int Page { get; init; } = 1;
    [BindFrom("period")] public string Period { get; init; } = "week";
}
public sealed class ActivityReportEndpoint(ISender sender) : Endpoint<ActivityReportRequest, UiReportSectionResponse<ActivityReport>>
{
    public override void Configure()
    {
        Get("/api/ui/reports/activity");
        AllowAnonymous();
        Tags("UI Reports");
    }
    public override async Task HandleAsync(ActivityReportRequest request, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new GetActivityReportQuery(request.Page, request.Period), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(Results.StatusCode(400)); return; }
        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class PreferencesReportRequest
{

}
public sealed class PreferencesReportEndpoint(ISender sender) : Endpoint<PreferencesReportRequest, UiReportSectionResponse<PreferenceReport>>
{
    public override void Configure()
    {
        Get("/api/ui/reports/preferences");
        AllowAnonymous();
        Tags("UI Reports");
    }
    public override async Task HandleAsync(PreferencesReportRequest request, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new GetPreferencesReportQuery(), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(Results.StatusCode(400)); return; }
        await Send.OkAsync(result.Value, ct);
    }
}

