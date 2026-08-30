using MessageBroker.Abstractions;

namespace IntegrationRequests;

public sealed record DisableProgramTaskProcessingRequest(
    string MarketingAddress,
    Guid CorrelationId,
    DateTime OccurredOnUtc) : IIntegrationRequest;
