using Ardalis.Result;
using MessageBroker.Abstractions;

namespace IntegrationRequests;

public interface IIntegrationRequestDispatcher
{
    Task<Result> DispatchAsync(
        IIntegrationRequest request,
        CancellationToken cancellationToken = default);
}
