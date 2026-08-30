namespace IntegrationRequests;

public sealed record ResetStructureActivaityRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(
        MarketingAddress,
        StructureNumber,
        CorrelationId,
        OccurredOnUtc);
