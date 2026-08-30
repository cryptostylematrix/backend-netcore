using MessageBroker.Abstractions;

namespace IntegrationRequests;

public sealed record EnableProgramTaskProcessingRequest(
    string MarketingAddress,
    Guid CorrelationId,
    DateTime OccurredOnUtc) : IIntegrationRequest;
