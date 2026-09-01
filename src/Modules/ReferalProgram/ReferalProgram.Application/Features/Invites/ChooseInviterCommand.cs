using Common.Domain;
using ReferalProgram.Application.Mappings;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Invites;

public sealed record ChooseInviterCommand(
    string MarketingAddr,
    string InviterAddr,
    string ProfileAddr,
    int TaskKey,
    long QueryId,
    string? SourceAddr,
    string ProfileLogin) : ICommand<CommandResponse>;

internal sealed class ChooseInviterCommandHandler(
    IPlaceQueries placeQueries,
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<ChooseInviterCommand, CommandResponse>
{
    private const byte StructureNumber = 0;
    private const uint PlaceNumber = 1;

    public async Task<Result<CommandResponse>> Handle(
        ChooseInviterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var structure = await structureQueries.GetStructureAsync(
                request.MarketingAddr,
                StructureNumber,
                cancellationToken);

            if (structure is null)
                return Result<CommandResponse>.Error("Structure was not found.");

            var inviter = await placeQueries.GetPlaceAsync(
                request.MarketingAddr,
                StructureNumber,
                request.InviterAddr,
                PlaceNumber,
                cancellationToken);

            if (inviter is null)
                return Result<CommandResponse>.Error("Inviter place was not found.");

            var existingInvite = await placeQueries.GetPlaceAsync(
                inviter.MarketingAddr,
                StructureNumber,
                request.ProfileAddr,
                PlaceNumber,
                cancellationToken);

            if (existingInvite is not null)
                return Result<CommandResponse>.Error("Invite is already created.");

            if (!inviter.IsActive)
                return Result<CommandResponse>.Error("Inviter is not active.");

            var inviterProfileAddr = inviter.ProfileAddr;
            if (string.IsNullOrWhiteSpace(inviterProfileAddr))
                return Result<CommandResponse>.Error("Inviter place has no profile address.");

            var pos = checked(inviter.Filling + 1);

            var parent = await placeRepository.GetByIdAsync(inviter.Id, cancellationToken);
            if (parent is null)
                return Result<CommandResponse>.Error("Inviter place was not found.");

            var createdPlace = Place.Create(
                parentId: inviter.Id,
                marketingAddr: inviter.MarketingAddr,
                structureNumber: inviter.StructNumber,
                profileAddr: request.ProfileAddr,
                profileLogin: request.ProfileLogin,
                index: request.ProfileLogin + PlaceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                placeNumber: PlaceNumber,
                parentProfileAddr: inviterProfileAddr,
                parentProfileLogin: inviter.ProfileLogin,
                parentPlaceNumber: inviter.PlaceNumber,
                mp: inviter.Mp + pos.ToString("X8"),
                posGroup: 0,
                kind: 0,
                pos: pos,
                filling: 0,
                deep: checked(inviter.Deep + 1),
                isActive: false,
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                activatedAt: null);

            placeRepository.Add(createdPlace);

            var source = await sourcePlaceResolver.ResolveAsync(
                createdPlace,
                structure.Height,
                cancellationToken);

            if (source is null)
                return Result<CommandResponse>.Error(
                    $"Could not find a parent at height {structure.Height}.");

            var response = new CommandResponse(
                source.Code,
                PlaceResponseMapper.Map(source.SourcePlace));

            createdPlace.RecordProcessedMarketingCommand(
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
