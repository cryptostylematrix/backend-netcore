using Common.Domain;
using ReferalProgram.Application.Mappings;
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
    BuyPlaceKind Kind,
    ChildPosition? ChildPosition) : ICommand<CommandResponse>;

internal sealed class BuyPlaceCommandHandler(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    IBuyPlacePolicy buyPlacePolicy,
    ISourcePlaceResolver sourcePlaceResolver,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<BuyPlaceCommand, CommandResponse>
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

            var requestedPosition = request.ChildPosition is null
                ? null
                : new RequestedPosition(
                    request.ChildPosition.Parent.StructureNumber,
                    request.ChildPosition.Parent.ProfileAddr,
                    request.ChildPosition.Parent.PlaceNumber,
                    request.ChildPosition.Position);

            var decision = await buyPlacePolicy.EvaluateAsync(
                request.MarketingAddr,
                request.StructureNumber,
                request.ProfileAddr,
                requestedPosition,
                cancellationToken);

            if (!decision.CanBuy || decision.Position is null)
                return Result<CommandResponse>.Error(
                    $"Place purchase is not allowed: {decision.Reason ?? "unknown_reason"}.");

            if (decision.IncludePosition && requestedPosition is null)
                return Result<CommandResponse>.Error(
                    "Place purchase is not allowed: position_is_required.");

            if (decision.Kind != request.Kind)
                return Result<CommandResponse>.Error(
                    "Place purchase is not allowed: buy_command_kind_mismatch.");

            var nextPosition = decision.Position;

            var parent = await placeRepository.GetAsync(
                request.MarketingAddr,
                request.StructureNumber,
                nextPosition.ProfileAddr,
                nextPosition.PlaceNumber,
                cancellationToken);

            if (parent is null)
                return Result<CommandResponse>.Error(
                    "Authorized parent place disappeared before execution.");

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
