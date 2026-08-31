using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Services;

public sealed class RequestedPositionResolver(IPlaceQueries placeQueries)
    : IRequestedPositionResolver
{
    public async Task<RequestedPositionResolution> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        byte structureWidth,
        byte positionGroup,
        RequestedPosition requestedPosition,
        string? requiredRootMp,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken)
    {
        if (requestedPosition.StructureNumber != structureNumber)
            return Denied("position_structure_mismatch");

        if (requestedPosition.Position == 0
            || (structureWidth > 0
                && requestedPosition.Position > structureWidth))
        {
            return Denied("position_is_outside_structure_width");
        }

        var parent = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            requestedPosition.ParentProfileAddr,
            requestedPosition.ParentPlaceNumber,
            cancellationToken);
        if (parent is null)
            return Denied("parent_place_not_found");

        if (parent.Kind == PlaceKinds.TerminalClone)
            return Denied("terminal_clone_cannot_have_children");

        if (requestedPosition.Position != checked(parent.Filling + 1))
            return Denied("position_is_not_parent_next_available");

        var requestedMp = parent.Mp
            + requestedPosition.Position.ToString("X8");
        if (requiredRootMp is not null
            && !requestedMp.StartsWith(requiredRootMp, StringComparison.Ordinal))
        {
            return Denied("position_is_outside_viewer_root");
        }

        if (lockMps.Any(lockMp =>
                requestedMp.StartsWith(lockMp, StringComparison.Ordinal)))
        {
            return Denied("position_is_locked");
        }

        return new RequestedPositionResolution(
            new NextPosResponse
            {
                Mp = requestedMp,
                PosGroup = positionGroup,
                ProfileAddr = parent.ProfileAddr,
                PlaceNumber = parent.PlaceNumber,
                Pos = requestedPosition.Position
            },
            Reason: null);
    }

    private static RequestedPositionResolution Denied(string reason) =>
        new(Position: null, reason);
}
