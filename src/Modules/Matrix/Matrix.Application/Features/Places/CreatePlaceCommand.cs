namespace Matrix.Application.Features.Places;

public sealed record CreatePlaceCommand(): IRequest<int>;

internal sealed class CreatePlaceCommandHandler : IRequestHandler<CreatePlaceCommand, int>
{
    public Task<int> Handle(CreatePlaceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}