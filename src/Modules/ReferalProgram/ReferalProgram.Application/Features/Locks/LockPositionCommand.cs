using Common.Domain;
using Contracts.Application.Features.ProfileItem;
using MediatR;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Locks;

public sealed record LockPositionCommand(
    string MarketingAddr,
    byte StructureNumber,
    byte PlaceStructureNumber,
    string ProfileAddr,
    string? PlaceProfileAddr,
    uint PlaceNumber,
    uint LockedPos,
    int TaskKey,
    long QueryId,
    string? SourceAddr) : ICommand<CommandResponse>;

internal sealed class LockPositionCommandHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    ISender sender,
    ITonAddressComparer addressComparer,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionRootResolver positionRootResolver,
    ILockQueries lockQueries,
    IPositionLockPolicy lockPolicy,
    IPlaceRepository placeRepository,
    IPositionLockRepository positionLockRepository,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<LockPositionCommand, CommandResponse>
{
    public async Task<Result<CommandResponse>> Handle(
        LockPositionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PlaceStructureNumber != request.StructureNumber)
            return Result<CommandResponse>.Error("Lock-position place belongs to a different structure.");

        if (string.IsNullOrWhiteSpace(request.SourceAddr))
            return Result<CommandResponse>.Error("Lock-position source address is required.");

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

        var placeAggregate = await placeRepository.GetByIdAsync(place.Id, cancellationToken);
        if (placeAggregate is null)
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

        if (decision.IsLock && decision.ViewerAuthorized)
        {
            var existingResponse = new CommandResponse(0, place);
            placeAggregate.RecordProcessedMarketingCommand(
                request.TaskKey,
                request.QueryId,
                request.SourceAddr,
                placeAggregate,
                existingResponse.Code,
                DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existingResponse);
        }

        if (!decision.CanLock)
            return Result<CommandResponse>.Error(
                $"Lock position is not allowed: {decision.Reason}.");

        var positionLock = PositionLock.Create(
            request.TaskKey,
            request.QueryId,
            request.SourceAddr,
            request.MarketingAddr,
            request.StructureNumber,
            place.ProfileAddr!,
            place.PlaceNumber,
            place.ProfileLogin!,
            request.ProfileAddr,
            request.LockedPos,
            lockMp,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        positionLockRepository.Add(positionLock);
        var response = new CommandResponse(0, place);
        placeAggregate.RecordProcessedMarketingCommand(
            request.TaskKey,
            request.QueryId,
            request.SourceAddr,
            placeAggregate,
            response.Code,
            DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(response);
    }
}
