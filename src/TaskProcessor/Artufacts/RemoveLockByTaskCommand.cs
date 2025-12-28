namespace Matrix.Application.Features.Locks;


public sealed record RemoveLockByTaskCommand(
    int TaskKey,
    long QueryId,
    short M,
    string ProfileAddr,
    string ProfileOwnerAddr,
    string SourceAddr,
    string ParentAddr,
    short Pos) : ICommand;

internal sealed class RemoveLockByTaskCommandHandler : ICommandHandler<RemoveLockByTaskCommand>
{
    public Task<Result> Handle(RemoveLockByTaskCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}