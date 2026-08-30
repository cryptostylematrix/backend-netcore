namespace IntegrationRequests;

public sealed record UpdateStructureActivityRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(
        MarketingAddress,
        StructureNumber,
        CorrelationId,
        OccurredOnUtc);
