using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

public sealed record CreateSystemCloneCommand(
    string MarketingAddr,
    byte StructureNumber,
    byte SourceStructureNumber,
    string? SourceProfileAddr,
    uint SourcePlaceNumber,
    ushort RelativeLevel,
    int TaskKey,
    long QueryId) : ICommand<CommandResponse>;

internal sealed class CreateSystemCloneCommandHandler(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    IRelativePlaceResolver relativePlaceResolver,
    INextPosService nextPosService,
    ISourcePlaceResolver sourcePlaceResolver,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSystemCloneCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        CreateSystemCloneCommand request,
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
                var relative = await relativePlaceResolver.ResolveAsync(
                    request.MarketingAddr,
                    request.SourceStructureNumber,
                    request.SourceProfileAddr,
                    request.SourcePlaceNumber,
                    request.RelativeLevel,
                    cancellationToken);

                if (relative is null)
                {
                    return Result<CommandResponse>.Error(
                        "An eligible relative place was not found.");
                }

                var profileAddr = relative.RelativePlace.ProfileAddr;
                var profileLogin = relative.RelativePlace.ProfileLogin;
                if (string.IsNullOrWhiteSpace(profileAddr)
                    || string.IsNullOrWhiteSpace(profileLogin))
                {
                    return Result<CommandResponse>.Error(
                        "The relative place has no profile identity.");
                }

                var nextPosition = await nextPosService.GetNextPosAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    profileAddr,
                    cancellationToken);

                if (nextPosition is null)
                    return Result<CommandResponse>.Error("No available position was found.");

                if (nextPosition.Pos == 0)
                    return Result<CommandResponse>.Error("The calculated position is invalid.");

                var parent = await placeRepository.GetAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    nextPosition.ProfileAddr,
                    nextPosition.PlaceNumber,
                    cancellationToken);

                if (parent is null)
                    return Result<CommandResponse>.Error("Parent place was not found.");

                var placeNumber = await placeRepository.GetNextPlaceNumberAsync(
                    request.MarketingAddr,
                    request.StructureNumber,
                    profileAddr,
                    cancellationToken);
                var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var createdPlace = Place.Create(
                    parentId: parent.Id,
                    marketingAddr: request.MarketingAddr,
                    structureNumber: request.StructureNumber,
                    profileAddr,
                    profileLogin,
                    index: profileLogin
                        + placeNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    placeNumber,
                    parentProfileAddr: parent.ProfileAddr,
                    parentProfileLogin: parent.ProfileLogin,
                    parentPlaceNumber: parent.PlaceNumber,
                    mp: nextPosition.Mp,
                    posGroup: nextPosition.PosGroup,
                    kind: 1,
                    pos: nextPosition.Pos,
                    filling: 0,
                    deep: checked(parent.Deep + 1),
                    isActive: true,
                    createdAt,
                    activatedAt: createdAt,
                    personalVolume: 0,
                    groupVolume: 0,
                    taskKey: request.TaskKey,
                    taskQueryId: request.QueryId,
                    taskSourceAddr: null);

                placeRepository.Add(createdPlace);
                placeWithTaskKey = createdPlace;
            }

            var source = await sourcePlaceResolver.ResolveAsync(
                placeWithTaskKey,
                structure.Height,
                cancellationToken);

            if (source is null)
            {
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");
            }

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
