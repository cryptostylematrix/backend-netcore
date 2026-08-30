namespace IntegrationRequests;

public sealed record ResetStructurePersonalVolumeRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(
        MarketingAddress,
        StructureNumber,
        CorrelationId,
        OccurredOnUtc);
