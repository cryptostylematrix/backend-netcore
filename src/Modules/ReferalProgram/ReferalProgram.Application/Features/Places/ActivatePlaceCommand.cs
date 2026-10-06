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

            var response = new CommandResponse(0, PlaceResponseMapper.Map(place));

            place.RecordProcessedMarketingCommand(
                request.TaskKey,
                request.QueryId,
                request.SourceAddr,
                place,
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
