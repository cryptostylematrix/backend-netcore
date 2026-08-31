using Common.Domain;
using ReferalProgram.Application.Mappings;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

public sealed record ActivatePlaceCommand(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    uint PlaceNumber,
    int TaskKey,
    long QueryId,
    string? SourceAddr) : ICommand<CommandResponse>;

internal sealed class ActivatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IActivatePlacePolicy activatePlacePolicy,
    IStructureQueries structureQueries,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<ActivatePlaceCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        ActivatePlaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = await activatePlacePolicy.EvaluateAsync(
                request.MarketingAddr,
                request.StructureNumber,
                request.ProfileAddr,
                request.PlaceNumber,
                cancellationToken);
            if (!decision.CanActivate)
            {
                return Result<CommandResponse>.Error(
                    $"Place activation is not allowed: {decision.Reason ?? "unknown_reason"}.");
            }

            var structure = await structureQueries.GetStructureAsync(
                request.MarketingAddr,
                request.StructureNumber,
                cancellationToken);
            if (structure is null)
                return Result<CommandResponse>.Error("Structure was not found.");

            var place = await placeRepository.GetAsync(
                request.MarketingAddr,
                request.StructureNumber,
                request.ProfileAddr,
                request.PlaceNumber,
                cancellationToken);
            if (place is null)
                return Result<CommandResponse>.Error("Place was not found.");

            var activatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            place.Activate(activatedAt, decision.SetActiveOnActivation);

            var source = await sourcePlaceResolver.ResolveAsync(
                place,
                structure.Height,
                cancellationToken);
            if (source is null)
            {
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");
            }

            var response = new CommandResponse(
                source.Code,
                PlaceResponseMapper.Map(source.SourcePlace));

            place.RecordProcessedMarketingCommand(
                request.TaskKey,
                request.QueryId,
                request.SourceAddr,
                source.SourcePlace,
                response.Code,
                DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<CommandResponse>.Error(exception.Message);
        }
    }

}
