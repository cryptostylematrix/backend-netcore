using Common.Domain;
using ReferalProgram.Application.Mappings;
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
    IRequestedPositionResolver requestedPositionResolver,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<BuySystemPlaceCommand, CommandResponse>
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
                var childPosition = request.ChildPosition;
                var resolution = await requestedPositionResolver.ResolveAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    structure.Width,
                    selection.Context.PosGroup,
                    new RequestedPosition(
                        childPosition.Parent.StructureNumber,
                        childPosition.Parent.ProfileAddr,
                        childPosition.Parent.PlaceNumber,
                        childPosition.Position),
                    requiredRootMp: null,
                    selection.Context.RootProfileLockMps,
                    cancellationToken);

                if (!resolution.IsSuccess)
                {
                    return Result<CommandResponse>.Error(
                        $"Requested position is not allowed: {resolution.Reason}.");
                }

                nextPosition = resolution.Position;
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

            if (parent.Kind == PlaceKinds.TerminalClone)
            {
                return Result<CommandResponse>.Error(
                    "A terminal clone cannot have children.");
            }

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
                boughtAt);

            placeRepository.Add(boughtPlace);

            var source = await sourcePlaceResolver.ResolveAsync(
                boughtPlace,
                structure.Height,
                cancellationToken);

            if (source is null)
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");

            var response = new CommandResponse(
                source.Code,
                PlaceResponseMapper.Map(source.SourcePlace));

            boughtPlace.RecordProcessedMarketingCommand(
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
