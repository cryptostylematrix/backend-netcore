using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;


public sealed record BuySystemPlaceCommand(
    string MarketingAddr,
    byte StructureNumber,
    int TaskKey,
    long QueryId,
    string? SourceAddr,
    ChildPosition? ChildPosition) : ICommand<CommandResponse>;

internal sealed class BuySystemPlaceCommandHandler(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    INextPosService nextPosService,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork) : ICommandHandler<BuySystemPlaceCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        BuySystemPlaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var structure = await structureQueries.GetStructureAsync(
                request.MarketingAddr,
                request.StructureNumber,
                cancellationToken);

            if (structure is null)
                return Result<CommandResponse>.Error("Structure was not found.");

            var placeWithTaskKey = await placeRepository.GetByTaskKeyAsync(
                request.MarketingAddr,
                request.TaskKey,
                cancellationToken);

            if (placeWithTaskKey is null)
            {
                var selection = await nextPosService.ResolveSelectionAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    null,
                    PositionOperation.BuySystemPlace,
                    cancellationToken);

                if (selection is null)
                    return Result<CommandResponse>.Error("No available position was found.");

                NextPosResponse? nextPosition;
                if (request.ChildPosition is not null
                    && selection.Algorithm.Equals(
                        "classic",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var requestedPosition = request.ChildPosition;
                    if (requestedPosition.Parent.StructureNumber
                        != request.StructureNumber)
                    {
                        return Result<CommandResponse>.Error(
                            "Requested parent belongs to a different structure.");
                    }

                    if (requestedPosition.Position == 0
                        || (structure.Width > 0
                            && requestedPosition.Position > structure.Width))
                    {
                        return Result<CommandResponse>.Error(
                            "Requested position is outside the structure width.");
                    }

                    var requestedParent = await placeRepository.GetAsync(
                        request.MarketingAddr,
                        request.StructureNumber,
                        requestedPosition.Parent.ProfileAddr,
                        requestedPosition.Parent.PlaceNumber,
                        cancellationToken);
                    if (requestedParent is null)
                        return Result<CommandResponse>.Error("Requested parent place was not found.");

                    if (requestedPosition.Position
                        != checked(requestedParent.Filling + 1))
                    {
                        return Result<CommandResponse>.Error(
                            "Requested position is not the parent's next available position.");
                    }

                    var requestedMp = requestedParent.Mp
                        + requestedPosition.Position.ToString("X8");
                    if (selection.Context.IsLocked(requestedMp))
                        return Result<CommandResponse>.Error("Requested position is locked.");

                    nextPosition = new NextPosResponse
                    {
                        Mp = requestedMp,
                        PosGroup = selection.Context.PosGroup,
                        ProfileAddr = requestedParent.ProfileAddr,
                        PlaceNumber = requestedParent.PlaceNumber,
                        Pos = requestedPosition.Position
                    };
                }
                else
                {
                    // Radar and Chess ignore a supplied position and calculate
                    // their own candidate.
                    nextPosition = await nextPosService.FindNextAsync(
                        selection,
                        cancellationToken);
                }

                if (nextPosition is null)
                    return Result<CommandResponse>.Error("No available position was found.");

                var parent = await placeRepository.GetAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    nextPosition.ProfileAddr,
                    nextPosition.PlaceNumber,
                    cancellationToken);

                if (parent is null)
                    return Result<CommandResponse>.Error("Parent place was not found.");

                if (nextPosition.Pos == 0)
                    return Result<CommandResponse>.Error("The calculated position is invalid.");

                var placeNumber = await placeRepository.GetNextPlaceNumberAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    null,
                    cancellationToken);

                var boughtAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var boughtPlace = Place.BuySystem(
                    parentId: parent.Id,
                    marketingAddr: request.MarketingAddr,
                    structureNumber: request.StructureNumber,
                    index: "system"
                        + placeNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    placeNumber,
                    parentProfileAddr: parent.ProfileAddr,
                    parentProfileLogin: parent.ProfileLogin,
                    parentPlaceNumber: parent.PlaceNumber,
                    mp: nextPosition.Mp,
                    posGroup: nextPosition.PosGroup,
                    kind: 0,
                    pos: nextPosition.Pos,
                    deep: checked(parent.Deep + 1),
                    boughtAt,
                    taskKey: request.TaskKey,
                    taskQueryId: request.QueryId,
                    taskSourceAddr: request.SourceAddr);

                placeRepository.Add(boughtPlace);
                placeWithTaskKey = boughtPlace;
            }

            var source = await sourcePlaceResolver.ResolveAsync(
                placeWithTaskKey,
                structure.Height,
                cancellationToken);

            if (source is null)
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");

            if (placeWithTaskKey.Id == 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new CommandResponse(
                source.Code,
                source.SourcePlace));
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
