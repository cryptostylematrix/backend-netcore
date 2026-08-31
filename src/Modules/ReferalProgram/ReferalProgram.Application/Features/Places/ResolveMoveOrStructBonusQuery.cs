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
            cancellationToken);

        if (relative?.RelativePlace.ProfileAddr is not { } profileAddr
            || string.IsNullOrWhiteSpace(profileAddr))
        {
            return Result<MoveOrStructBonusDecision>.Error(
                "An eligible relative profile place was not found.");
        }

        var placesCount = await placeQueries.GetPlacesCountAsync(
            request.MarketingAddr,
            request.TargetStructureNumber,
            profileAddr,
            cancellationToken);

        return Result.Success(new MoveOrStructBonusDecision(
            CreateClone: placesCount == 0));
    }
}
