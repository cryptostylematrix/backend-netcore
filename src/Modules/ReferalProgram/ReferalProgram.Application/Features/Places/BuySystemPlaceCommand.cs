using System.Text.Json;
using System.Text.Json.Serialization;
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
    IUnitOfWork unitOfWork) : ICommandHandler<BuySystemPlaceCommand, CommandResponse>
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
                var posAlgo = structure.PosAlgo.Deserialize<PositionAlgorithm>()
                    ?? throw new InvalidOperationException("Structure pos_algo is empty or invalid.");

                var root = posAlgo.Root?.ToLowerInvariant();
                if (root == "owner" && request.ChildPosition is not null)
                {
                    return Result<CommandResponse>.Error(
                        "A child position cannot be provided for an owner-rooted structure.");
                }

                if (root is not ("owner" or "profile"))
                    return Result<CommandResponse>.Error($"Unknown pos_algo root '{posAlgo.Root}'.");

                var nextPosition = await nextPosService.GetNextPosAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    null,
                    cancellationToken);

                if (nextPosition is null)
                    return Result<CommandResponse>.Error("No available position was found.");

                if (root == "profile" && request.ChildPosition is not null)
                {
                    // TODO: Check the requested ChildPosition against the calculated next position.
                }

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

    private sealed class PositionAlgorithm
    {
        [JsonPropertyName("root")]
        public string? Root { get; init; }
    }
}
