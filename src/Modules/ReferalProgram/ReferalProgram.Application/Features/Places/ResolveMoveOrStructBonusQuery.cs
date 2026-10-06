namespace ReferalProgram.Application.Features.Places;

public sealed record ResolveMoveOrStructBonusQuery(
    string MarketingAddr,
    byte TargetStructureNumber,
    byte SourceStructureNumber,
    string? SourceProfileAddr,
    uint SourcePlaceNumber,
    ushort RelativeLevel) : IQuery<MoveOrStructBonusDecision>;

public sealed record MoveOrStructBonusDecision(bool CreateClone);

internal sealed class ResolveMoveOrStructBonusQueryHandler(
    IPlaceQueries placeQueries,
    IRelativePlaceResolver relativePlaceResolver)
    : IQueryHandler<ResolveMoveOrStructBonusQuery, MoveOrStructBonusDecision>
{
    public async Task<Result<MoveOrStructBonusDecision>> Handle(
        ResolveMoveOrStructBonusQuery request,
        CancellationToken cancellationToken)
    {
        var relative = await relativePlaceResolver.ResolveAsync(
            request.MarketingAddr,
            request.SourceStructureNumber,
            request.SourceProfileAddr,
            request.SourcePlaceNumber,
            request.RelativeLevel,
            cancellationToken,
            RecipientPurpose.Clone);

        if (relative?.RelativePlace.ProfileAddr is { } profileAddr
            && !string.IsNullOrWhiteSpace(profileAddr))
        {
            var placesCount = await placeQueries.GetPlacesCountAsync(
                request.MarketingAddr,
                request.TargetStructureNumber,
                profileAddr,
                cancellationToken);

            if (placesCount == 0)
                return Result.Success(new MoveOrStructBonusDecision(CreateClone: true));
        }

        // The bonus branch may legitimately resolve a different recipient.
        var bonus = await relativePlaceResolver.ResolveAsync(
            request.MarketingAddr,
            request.SourceStructureNumber,
            request.SourceProfileAddr,
            request.SourcePlaceNumber,
            request.RelativeLevel,
            cancellationToken,
            RecipientPurpose.Bonus);
        if (string.IsNullOrWhiteSpace(bonus?.RelativePlace.ProfileAddr))
            return Result<MoveOrStructBonusDecision>.Error("An eligible relative profile place was not found.");

        return Result.Success(new MoveOrStructBonusDecision(CreateClone: false));
    }
}
