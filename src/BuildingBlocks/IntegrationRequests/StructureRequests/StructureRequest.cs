using MessageBroker.Abstractions;

namespace IntegrationRequests;

public abstract record StructureRequest(
    string MarketingAddress,
    int StructureNumber,
    Guid CorrelationId,
    DateTime OccurredOnUtc) : IIntegrationRequest;
