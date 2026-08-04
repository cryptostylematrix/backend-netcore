namespace ReferalProgram.Application.Features.Invites;

public sealed record ChooseInviterCommand(
    string MarketingAddr,
    string InviterAddr,
    string ProfileAddr,
    int TaskKey,
    long QueryId,
    string? SourceAddr,
    string ProfileLogin) : ICommand<PlaceResponse>;

internal sealed class ChooseInviterCommandHandler(
    IPlaceQueries placeQueries,
    IPlaceCommands placeCommands) : ICommandHandler<ChooseInviterCommand, PlaceResponse>
{
    private const byte StructureNumber = 0;
    private const uint PlaceNumber = 1;

    public async Task<Result<PlaceResponse>> Handle(
        ChooseInviterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var inviter = await placeQueries.GetPlaceAsync(
                request.MarketingAddr,
                StructureNumber,
                request.InviterAddr,
                PlaceNumber,
                cancellationToken);

            if (inviter is null)
                return Result<PlaceResponse>.Error("Inviter place was not found.");

            var taskPlace = await placeQueries.GetPlaceByTaskKeyAsync(
                inviter.MarketingAddr,
                request.TaskKey,
                cancellationToken);

            if (taskPlace is not null)
                return Result<PlaceResponse>.Error("Invite for this task is already created.");

            var existingInvite = await placeQueries.GetPlaceAsync(
                inviter.MarketingAddr,
                StructureNumber,
                request.ProfileAddr,
                PlaceNumber,
                cancellationToken);

            if (existingInvite is not null)
                return Result<PlaceResponse>.Error("Invite is already created.");

            if (!inviter.IsActive)
                return Result<PlaceResponse>.Error("Inviter is not active.");

            var inviterProfileAddr = inviter.ProfileAddr;
            if (string.IsNullOrWhiteSpace(inviterProfileAddr))
                return Result<PlaceResponse>.Error("Inviter place has no profile address.");

            var pos = checked(inviter.Filling + 1);

            var createdPlace = await placeCommands.CreatePlaceAsync(
                new CreatePlaceCommand(
                    ParentId: inviter.Id,
                    ParentFilling: inviter.Filling,
                    MarketingAddr: inviter.MarketingAddr,
                    StructureNumber: inviter.StructNumber,
                    ProfileAddr: request.ProfileAddr,
                    ProfileLogin: request.ProfileLogin,
                    Index: request.ProfileLogin + PlaceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    PlaceNumber: PlaceNumber,
                    ParentProfileAddr: inviterProfileAddr,
                    ParentProfileLogin: inviter.ProfileLogin,
                    ParentPlaceNumber: inviter.PlaceNumber,
                    Mp: inviter.Mp + pos.ToString("X8"),
                    PosGroup: 0,
                    Kind: 0,
                    Pos: pos,
                    Filling: 0,
                    Deep: checked(inviter.Deep + 1),
                    IsActive: false,
                    CreatedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ActivatedAt: null,
                    PersonalVolume: 0,
                    GroupVolume: 0,
                    TaskKey: request.TaskKey,
                    TaskQueryId: request.QueryId,
                    TaskSourceAddr: request.SourceAddr),
                cancellationToken);

            return Result.Success(createdPlace);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<PlaceResponse>.Error(exception.Message);
        }
    }
}
