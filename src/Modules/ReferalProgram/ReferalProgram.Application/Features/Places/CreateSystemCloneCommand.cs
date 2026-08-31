using Common.Domain;
using ReferalProgram.Application.Mappings;
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
    long QueryId,
    PositionOperation Operation) : ICommand<CommandResponse>;

internal sealed class CreateSystemCloneCommandHandler(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    IRelativePlaceResolver relativePlaceResolver,
    INextPosService nextPosService,
    IClonePlaceKindPolicy clonePlaceKindPolicy,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<CreateSystemCloneCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        CreateSystemCloneCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Operation is not PositionOperation.CreateClone
                and not PositionOperation.CreateReinvest)
            {
                return Result<CommandResponse>.Error(
                    $"Position operation '{request.Operation}' is not valid for clone creation.");
            }

            var structure = await structureQueries.GetStructureAsync(
                request.MarketingAddr,
                request.StructureNumber,
                cancellationToken);

            if (structure is null)
                return Result<CommandResponse>.Error("Structure was not found.");

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

            var positionSelection = await nextPosService.ResolveSelectionAsync(
                request.MarketingAddr,
                request.StructureNumber,
                profileAddr,
                request.Operation,
                cancellationToken);

            if (positionSelection is null)
                return Result<CommandResponse>.Error("No available position was found.");

            var nextPosition = await nextPosService.FindNextAsync(
                positionSelection,
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

            var placeKind = await clonePlaceKindPolicy.ResolveAsync(
                positionSelection,
                parent.Id,
                cancellationToken);

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
                kind: placeKind,
                pos: nextPosition.Pos,
                filling: 0,
                deep: checked(parent.Deep + 1),
                isActive: true,
                createdAt,
                activatedAt: createdAt,
                personalVolume: 0,
                groupVolume: 0);

            placeRepository.Add(createdPlace);
            createdPlace.EnsurePaidPlaceEffects();

            var source = await sourcePlaceResolver.ResolveAsync(
                createdPlace,
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

            createdPlace.RecordProcessedMarketingCommand(
                request.TaskKey,
                request.QueryId,
                taskSourceAddr: null,
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
