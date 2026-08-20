using Common.Domain;
using Contracts.Application.Features.ProfileItem;
using MediatR;
using ReferalProgram.Core.LockAggregate;

namespace ReferalProgram.Application.Features.Locks;

public sealed record UnlockPositionCommand(
    string MarketingAddr,
    byte StructureNumber,
    byte PlaceStructureNumber,
    string ProfileAddr,
    string? PlaceProfileAddr,
    uint PlaceNumber,
    uint LockedPos,
    string? SourceAddr) : ICommand<CommandResponse>;

internal sealed class UnlockPositionCommandHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    ISender sender,
    ITonAddressComparer addressComparer,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionRootResolver positionRootResolver,
    ILockQueries lockQueries,
    IPositionLockPolicy lockPolicy,
    IPositionLockRepository positionLockRepository,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<UnlockPositionCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        UnlockPositionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PlaceStructureNumber != request.StructureNumber)
            return Result<CommandResponse>.Error("Unlock-position place belongs to a different structure.");

        if (string.IsNullOrWhiteSpace(request.SourceAddr))
            return Result<CommandResponse>.Error("Unlock-position source address is required.");

        var profileResult = await sender.Send(
            new GetNftDataQuery(request.ProfileAddr),
            cancellationToken);

        if (!profileResult.IsSuccess
            || string.IsNullOrWhiteSpace(profileResult.Value.OwnerAddr))
        {
            return Result<CommandResponse>.Error("Could not load the profile owner.");
        }

        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);
        if (structure is null)
            return Result<CommandResponse>.Error("Structure was not found.");

        var configuration = configurationParser.Parse(structure.PosAlgo);

        var root = await positionRootResolver.ResolveAsync(
            configuration.Root,
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken);
        if (root is null)
            return Result<CommandResponse>.Error("Could not resolve the profile root place.");

        var place = await placeQueries.GetPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.PlaceProfileAddr,
            request.PlaceNumber,
            cancellationToken);
        if (place is null)
            return Result<CommandResponse>.Error("Place was not found.");

        var lockMps = await lockQueries.GetAllLockMpsAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken);
        var lockMp = place.Mp + request.LockedPos.ToString("X8");
        var decision = lockPolicy.Evaluate(
            new PositionLockContext(
                structure.Width,
                root.Mp,
                lockMps,
                addressComparer.AreEqual(request.SourceAddr, profileResult.Value.OwnerAddr),
                CanIssueLockCommand: true,
                CanIssueUnlockCommand: true),
            place,
            lockMp,
            request.LockedPos);

        if (!decision.ViewerAuthorized)
            return Result<CommandResponse>.Error(
                $"Unlock position is not allowed: {decision.Reason}.");

        if (!decision.IsLock)
            return Result.Success(new CommandResponse(0, place));

        if (!decision.CanUnlock)
            return Result<CommandResponse>.Error(
                $"Unlock position is not allowed: {decision.Reason}.");

        var positionLock = await positionLockRepository.GetAsync(
            request.MarketingAddr,
            request.StructureNumber,
            place.ProfileAddr!,
            place.PlaceNumber,
            request.ProfileAddr,
            request.LockedPos,
            cancellationToken);

        if (positionLock is null)
            return Result.Success(new CommandResponse(0, place));

        positionLockRepository.Remove(positionLock);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new CommandResponse(0, place));
    }
}
