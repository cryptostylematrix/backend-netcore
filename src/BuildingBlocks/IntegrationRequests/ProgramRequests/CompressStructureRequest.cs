namespace IntegrationRequests;

public sealed record CompressStructureRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc)
    : StructureRequest(
        MarketingAddress,
        StructureNumber,
        CorrelationId,
        OccurredOnUtc);
