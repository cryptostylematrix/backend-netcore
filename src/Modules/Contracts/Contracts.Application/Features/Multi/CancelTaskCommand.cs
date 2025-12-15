namespace Contracts.Application.Features.Multi;

public sealed record CancelTaskCommand(): ICommand;

internal sealed class CancelTaskCommandHandler(ITonClient tonClient) : 
    ICommandHandler<CancelTaskCommand>
{
    public Task<Result> Handle(CancelTaskCommand request, CancellationToken ct)
    {
        return Task.FromResult(Result.Success());
    }
}