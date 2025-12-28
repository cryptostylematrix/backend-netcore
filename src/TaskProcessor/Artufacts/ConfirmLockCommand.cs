namespace Matrix.Application.Features.Locks;

public sealed record ConfirmLockCommand() : ICommand;

internal sealed class ConfirmLockCommandHandler : ICommandHandler<ConfirmLockCommand>
{
    public Task<Result> Handle(ConfirmLockCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}