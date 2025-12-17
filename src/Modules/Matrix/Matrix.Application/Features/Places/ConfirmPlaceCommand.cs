namespace Matrix.Application.Features.Places;

public sealed record ConfirmPlaceCommand(): ICommand;

internal sealed class ConfirmPlaceCommandHandler : ICommandHandler<ConfirmPlaceCommand>
{
    public Task<Result> Handle(ConfirmPlaceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}