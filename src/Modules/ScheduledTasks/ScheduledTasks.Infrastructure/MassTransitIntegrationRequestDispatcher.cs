using Ardalis.Result;
using IntegrationRequests;
using MassTransit;
using MessageBroker.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace ScheduledTasks.Infrastructure;

public sealed class MassTransitIntegrationRequestDispatcher(IClientFactory clientFactory)
    : IIntegrationRequestDispatcher
{
    private static readonly MethodInfo DispatchTypedMethod =
        typeof(MassTransitIntegrationRequestDispatcher).GetMethod(
            nameof(DispatchTypedAsync),
            BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, MethodInfo> ClosedDispatchMethods = [];

    public Task<Result> DispatchAsync(
        IIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var method = ClosedDispatchMethods.GetOrAdd(
            request.GetType(),
            requestType => DispatchTypedMethod.MakeGenericMethod(requestType));
        return (Task<Result>)method.Invoke(this, [request, cancellationToken])!;
    }

    private async Task<Result> DispatchTypedAsync<TRequest>(
        IIntegrationRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IIntegrationRequest
    {
        try
        {
            var client = clientFactory.CreateRequestClient<TRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                (TRequest)request,
                cancellationToken);
            return response.Message.Errors is { Length: > 0 } errors
                ? Result.Error(string.Join("; ", errors))
                : Result.Success();
        }
        catch (RequestFaultException exception)
        {
            return Result.Error(exception.Message);
        }
    }
}
