using MessageBroker.Abstractions;

namespace IntegrationRequests;

public sealed record IntegrationRequestResponse(string[]? Errors)
    : IIntegrationResponse;
