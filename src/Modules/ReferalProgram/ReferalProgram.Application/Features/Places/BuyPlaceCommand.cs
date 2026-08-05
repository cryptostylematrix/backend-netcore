using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

public sealed record BuyPlaceRef(
    byte StructureNumber,
    string? ProfileAddr,
    uint PlaceNumber);

public sealed record ChildPosition(
    BuyPlaceRef Parent,
    uint Position);

public sealed record BuyPlaceCommand(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    string ProfileLogin,
    int TaskKey,
    long QueryId,
    string? SourceAddr,
    ChildPosition? ChildPosition) : ICommand<CommandResponse>;

internal sealed class BuyPlaceCommandHandler(
    IPlaceRepository placeRepository,
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    INextPosService nextPosService,
    IUnitOfWork unitOfWork) : ICommandHandler<BuyPlaceCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        BuyPlaceCommand request,
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
                var actualPlacesCount = await placeQueries.GetPlacesCountAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    request.ProfileAddr,
                    cancellationToken);

                if (actualPlacesCount >= structure.MaxPlacesPerProfile)
                {
                    return Result<CommandResponse>.Error(
                        $"The profile already has the maximum number of places ({structure.MaxPlacesPerProfile}).");
                }

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
                    request.ProfileAddr,
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
                    request.ProfileAddr,
                    cancellationToken);

                var boughtAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var boughtPlace = Place.Buy(
                    parentId: parent.Id,
                    marketingAddr: request.MarketingAddr,
                    structureNumber: request.StructureNumber,
                    profileAddr: request.ProfileAddr,
                    profileLogin: request.ProfileLogin,
                    index: request.ProfileLogin
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
                await unitOfWork.SaveChangesAsync(cancellationToken);
                placeWithTaskKey = boughtPlace;
            }
            
            var matrixTopPlace = await placeRepository.GetAncestorAsync(
                placeWithTaskKey,
                structure.Height,
                cancellationToken);

            if (matrixTopPlace is null)
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");

            var matrixPlacesCount = await placeRepository.CountAtDepthAsync(
                request.MarketingAddr,
                request.StructureNumber,
                matrixTopPlace.Mp,
                placeWithTaskKey.Deep,
                cancellationToken);

            return Result.Success(new CommandResponse(
                Code: checked((uint)matrixPlacesCount),
                Source: ToResponse(matrixTopPlace)));
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

    private static PlaceResponse ToResponse(Place place) => new()
    {
        Id = place.Id,
        ParentId = place.ParentId,
        Mp = place.Mp,
        PosGroup = place.PosGroup,
        MarketingAddr = place.MarketingAddr,
        StructNumber = place.StructureNumber,
        ProfileAddr = place.ProfileAddr,
        PlaceNumber = place.PlaceNumber,
        ProfileLogin = place.ProfileLogin,
        Index = place.Index,
        ParentProfileAddr = place.ParentProfileAddr,
        ParentProfileLogin = place.ParentProfileLogin,
        ParentPlaceNumber = place.ParentPlaceNumber,
        CreatedAt = place.CreatedAt,
        ActivatedAt = place.ActivatedAt,
        IsActive = place.IsActive,
        Kind = place.Kind,
        Pos = place.Pos,
        Filling = place.Filling,
        Deep = place.Deep,
        PersonalVolume = place.PersonalVolume,
        GroupVolume = place.GroupVolume
    };

    private sealed class PositionAlgorithm
    {
        [JsonPropertyName("root")]
        public string? Root { get; init; }
    }
}
