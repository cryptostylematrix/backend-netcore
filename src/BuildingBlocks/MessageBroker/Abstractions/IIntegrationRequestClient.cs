namespace MessageBroker.Abstractions;

public interface IIntegrationRequestClient<in TRequest> where TRequest : class, IIntegrationRequest
{
    Task<TResponse> GetResponse<TResponse>(
        TRequest request, CancellationToken cancellationToken = default) where TResponse : class, IIntegrationResponse;
}