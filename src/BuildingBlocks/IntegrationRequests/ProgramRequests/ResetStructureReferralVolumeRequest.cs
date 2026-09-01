namespace IntegrationRequests;

public sealed record ResetStructureReferralVolumeRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(
        MarketingAddress,
        StructureNumber,
        CorrelationId,
        OccurredOnUtc);
