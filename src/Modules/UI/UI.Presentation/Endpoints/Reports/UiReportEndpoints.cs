using Microsoft.AspNetCore.Http;
using UI.Application.Features.Reports;

namespace UI.Presentation.Endpoints.Reports;

public sealed class UiReportRequest
{
    [BindFrom("profile_page")] public int ProfilePage { get; init; } = 1;
    [BindFrom("activity_page")] public int ActivityPage { get; init; } = 1;
    [BindFrom("period")] public string Period { get; init; } = "week";
    [BindFrom("group_contract")] public bool GroupContract { get; init; } = true;
    [BindFrom("group_wallet_name")] public bool GroupWalletName { get; init; }
    [BindFrom("group_app_version")] public bool GroupAppVersion { get; init; }
    [BindFrom("group_platform")] public bool GroupPlatform { get; init; }
}
public sealed class UiReportEndpoint(ISender sender) : Endpoint<UiReportRequest, UiReportResponse>
{
    public override void Configure()
    {
        Get("/api/ui/reports");
        AllowAnonymous();
        Tags("UI Reports");
    }
    public override async Task HandleAsync(UiReportRequest request, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new GetUiReportQuery( request.ProfilePage, request.ActivityPage,
            request.Period, request.GroupContract, request.GroupWalletName, request.GroupAppVersion, request.GroupPlatform), ct);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(Results.StatusCode(400));
            return;
        }
        await Send.OkAsync(result.Value, ct);
    }
}
