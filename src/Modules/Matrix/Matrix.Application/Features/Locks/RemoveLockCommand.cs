namespace Matrix.Application.Features.Locks;

public sealed record RemoveLockCommand(): ICommand;

internal sealed class RemoveLockCommandHandler : ICommandHandler<RemoveLockCommand>
{
    public Task<Result> Handle(RemoveLockCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}