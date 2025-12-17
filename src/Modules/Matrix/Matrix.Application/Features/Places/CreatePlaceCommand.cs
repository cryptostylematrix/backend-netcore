namespace Matrix.Application.Features.Places;

public sealed record CreatePlaceCommand(): ICommand<int>;

internal sealed class CreatePlaceCommandHandler : ICommandHandler<CreatePlaceCommand, int>
{
    public Task<Result<int>> Handle(CreatePlaceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}