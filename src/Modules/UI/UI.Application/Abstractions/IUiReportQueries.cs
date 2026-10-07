using UI.Dto;

namespace UI.Application.Abstractions;

public sealed record UiReportFilter(int ProfilePage, int ActivityPage, DateTime From, DateTime To,
    bool GroupContract, bool GroupWalletName, bool GroupAppVersion, bool GroupPlatform);

public interface IUiReportQueries
{
    Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken cancellationToken);
}
