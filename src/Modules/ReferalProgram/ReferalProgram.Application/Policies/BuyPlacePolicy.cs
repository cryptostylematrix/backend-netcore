namespace ReferalProgram.Application.Policies;

public sealed class BuyPlacePolicy(
    IStructureQueries structureQueries,
    IPlaceQueries placeQueries,
    ILockQueries lockQueries,
    INextPosService nextPosService,
    IPositionRootResolver positionRootResolver,
    IPositionAlgorithmConfigurationParser configurationParser,
    IProgramCommandQueries commandQueries) : IBuyPlacePolicy
{
    public BuyPositionDecision EvaluatePosition(
        BuyPlaceDecision decision,
        PlaceResponse? parent,
        string mp,
        uint position,
        bool isLocked)
    {
        var positionAllowed = decision.RequireNextPosition
            ? string.Equals(mp, decision.Position?.Mp, StringComparison.Ordinal)
            : decision.ViewerRootMp is not null
              && mp.StartsWith(decision.ViewerRootMp, StringComparison.Ordinal)
              && parent is not null
              && position == checked(parent.Filling + 1);

        if (!decision.CanBuy || isLocked || !positionAllowed || parent is null)
            return new BuyPositionDecision(false, null);

        var command = SelectCommand(
            decision.PlacesCount,
            decision.AvailableCommandTags);

        return command is null
            ? new BuyPositionDecision(false, null)
            : new BuyPositionDecision(true, command.Value.Tag);
    }

    public async Task<BuyPlaceDecision> EvaluateAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        RequestedPosition? requestedPosition,
        CancellationToken cancellationToken)
    {
        var structure = await structureQueries.GetStructureAsync(
            marketingAddr,
            structureNumber,
            cancellationToken);

        if (structure is null)
            return Denied("structure_not_found");

        var configuration = configurationParser.Parse(structure.PosAlgo);
        var requireNextPosition = configuration.Root.Equals(
            "owner",
            StringComparison.OrdinalIgnoreCase);
        var placesCount = await placeQueries.GetPlacesCountAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            cancellationToken);

        if (structure.MaxPlacesPerProfile > 0
            && placesCount >= structure.MaxPlacesPerProfile)
        {
            return Denied("max_places_reached");
        }

        var nextPosition = await nextPosService.GetNextPosAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            cancellationToken);

        if (nextPosition is null)
            return Denied("no_available_position");

        if (nextPosition.Pos == 0)
            return Denied("calculated_position_is_invalid");

        var viewerRoot = await positionRootResolver.ResolveAsync(
            configuration.Root,
            marketingAddr,
            structureNumber,
            profileAddr,
            cancellationToken);
        if (viewerRoot is null)
            return Denied("viewer_root_not_found");

        var selectedPosition = nextPosition;

        if (requestedPosition is not null)
        {
            if (requestedPosition.StructureNumber != structureNumber)
                return Denied("position_structure_mismatch");

            if (requireNextPosition)
                return Denied("position_not_allowed");

            if (requestedPosition.Position == 0
                || (structure.Width > 0 && requestedPosition.Position > structure.Width))
            {
                return Denied("position_is_outside_structure_width");
            }

            var requestedParent = await placeQueries.GetPlaceAsync(
                marketingAddr,
                structureNumber,
                requestedPosition.ParentProfileAddr,
                requestedPosition.ParentPlaceNumber,
                cancellationToken);
            if (requestedParent is null)
                return Denied("parent_place_not_found");

            if (requestedPosition.Position != checked(requestedParent.Filling + 1))
                return Denied("position_is_not_parent_next_available");

            var requestedMp = requestedParent.Mp
                + requestedPosition.Position.ToString("X8");
            if (!requestedMp.StartsWith(viewerRoot.Mp, StringComparison.Ordinal))
                return Denied("position_is_outside_viewer_root");

            var lockMps = await lockQueries.GetAllLockMpsAsync(
                marketingAddr,
                structureNumber,
                profileAddr,
                cancellationToken);
            if (lockMps.Any(lockMp =>
                    requestedMp.StartsWith(lockMp, StringComparison.Ordinal)))
            {
                return Denied("position_is_locked");
            }

            selectedPosition = new NextPosResponse
            {
                Mp = requestedMp,
                PosGroup = nextPosition.PosGroup,
                ProfileAddr = requestedParent.ProfileAddr,
                PlaceNumber = requestedParent.PlaceNumber,
                Pos = requestedPosition.Position
            };
        }

        var parent = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            selectedPosition.ProfileAddr,
            selectedPosition.PlaceNumber,
            cancellationToken);
        if (parent is null)
            return Denied("parent_place_not_found");

        var commandTags = await commandQueries.GetAvailableCommandTagsAsync(
            marketingAddr,
            structureNumber,
            cancellationToken);
        var command = SelectCommand(
            placesCount,
            commandTags);
        if (command is null)
            return Denied("buy_command_not_configured");

        return new BuyPlaceDecision(
            CanBuy: true,
            command.Value.Kind,
            command.Value.Tag,
            IncludePosition: !requireNextPosition,
            Position: selectedPosition,
            Reason: null)
        {
            RequireNextPosition = requireNextPosition,
            ViewerRootMp = viewerRoot.Mp,
            PlacesCount = placesCount,
            AvailableCommandTags = commandTags
        };
    }

    private static (BuyPlaceKind Kind, uint Tag)? SelectCommand(
        long placesCount,
        IReadOnlySet<uint> availableTags)
    {
        if (placesCount == 0 && availableTags.Contains(ProgramCommandTags.BuyFirstPlace))
            return (BuyPlaceKind.First, ProgramCommandTags.BuyFirstPlace);

        return availableTags.Contains(ProgramCommandTags.BuyPlace)
            ? (BuyPlaceKind.Regular, ProgramCommandTags.BuyPlace)
            : null;
    }

    private static BuyPlaceDecision Denied(string reason) =>
        new(false, null, null, false, null, reason);
}
