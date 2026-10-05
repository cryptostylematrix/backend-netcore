namespace IntegrationRequests;

public sealed record DeactivateExpiredFirstPlacesRequest(
    string MarketingAddress,
    int StructureNumber,
    ActivityExpirationPeriod Period,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(MarketingAddress, StructureNumber, CorrelationId, OccurredOnUtc)
{
    public const string CommandType = "program.structure.deactivate-expired-first-places";
}
