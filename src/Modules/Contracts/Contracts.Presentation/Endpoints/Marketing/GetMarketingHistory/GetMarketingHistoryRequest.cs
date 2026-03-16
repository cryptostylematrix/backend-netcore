namespace Contracts.Presentation.Endpoints.Marketing.GetMarketingHistory;

public sealed class GetMarketingHistoryRequest
{
    public string Addr { get; init; } = null!;
    public uint Limit { get; init; } = 10;
    public ulong? Lt { get; init; } = null;
    public string? Hash { get; init; } = null;
}