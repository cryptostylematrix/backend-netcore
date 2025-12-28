namespace Matrix.Application.Features.Places;

public sealed record CreateClonePlaceByTaskCommand(int TaskKey, long QueryId, short M, string ProfileAddr): ICommand;

internal sealed class CreateClonePlaceByTaskCommandHandler : ICommandHandler<CreateClonePlaceByTaskCommand>
{
    public Task<Result> Handle(CreateClonePlaceByTaskCommand request, CancellationToken cancellationToken)
    {
        
        
        
        // var place = MultiPlace.CreateMultiPlace(
        //     
        //     
        //     )
        
        
        throw new NotImplementedException();
    }
}