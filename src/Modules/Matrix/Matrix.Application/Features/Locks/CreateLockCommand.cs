namespace Matrix.Application.Features.Locks;

public sealed record CreateLockCommand() : ICommand<int>;

internal sealed class CreateLockCommandHandler : ICommandHandler<CreateLockCommand, int>
{
    public Task<Result<int>> Handle(CreateLockCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}