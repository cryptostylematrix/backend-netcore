namespace Matrix.Application.Features.Places;

public sealed record ConfirmPlaceCommand(): IRequest;

internal sealed class ConfirmPlaceCommandHandler : IRequestHandler<ConfirmPlaceCommand>
{
    public Task Handle(ConfirmPlaceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}