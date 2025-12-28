namespace MessageBroker.Abstractions;

public interface IIntegrationResponse
{
    public string[]? Errors { get; init; }
}