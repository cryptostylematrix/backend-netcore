using UI.Dto;

namespace UI.Application.Abstractions;

public sealed record UiReportFilter(int ProfilePage, int ActivityPage, DateTime From, DateTime To,
    bool GroupContract, bool GroupWalletName, bool GroupAppVersion, bool GroupPlatform);

public interface IUiReportQueries
{
    Task<ProfileReport> GetProfilesAsync(UiReportFilter filter, CancellationToken cancellationToken);
    Task<ConnectionReport> GetTonConnectAsync(UiReportFilter filter, CancellationToken cancellationToken);
    Task<ActivityReport> GetActivityAsync(UiReportFilter filter, CancellationToken cancellationToken);
    Task<PreferenceReport> GetPreferencesAsync(UiReportFilter filter, CancellationToken cancellationToken);
    Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken cancellationToken);
}
