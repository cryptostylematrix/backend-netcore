namespace ReferalProgram.Application.Policies;

public sealed class BuyPlacePolicy(
    IStructureQueries structureQueries,
    IPlaceQueries placeQueries,
    ILockQueries lockQueries,
    INextPosService nextPosService,
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
            decision.HasPlacesInBuyFirstPlaceStructures,
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

        if (structure.PrevRequired)
        {
            if (structureNumber == 0)
                return Denied("previous_structure_does_not_exist");

            var previousStructurePlacesCount =
                await placeQueries.GetPlacesCountAsync(
                    marketingAddr,
                    checked((byte)(structureNumber - 1)),
                    profileAddr,
                    cancellationToken);

            if (previousStructurePlacesCount == 0)
                return Denied("previous_structure_place_required");
        }

        var commandConfiguration = await commandQueries.GetConfigurationAsync(
            marketingAddr,
            cancellationToken);
        var buyFirstPlaceStructures = commandConfiguration.GetStructureNumbers(
            ProgramCommandTags.BuyFirstPlace);
        var hasPlacesInBuyFirstPlaceStructures = buyFirstPlaceStructures.Count > 0
            && await placeQueries.HasProfilePlacesInStructuresAsync(
                marketingAddr,
                profileAddr,
                buyFirstPlaceStructures,
                cancellationToken);

        var commandTags = commandConfiguration.GetAvailableCommandTags(
            structureNumber);
        var command = SelectCommand(
            hasPlacesInBuyFirstPlaceStructures,
            commandTags);
        if (command is null)
            return Denied("buy_command_not_configured");

        var operation = command.Value.Kind == BuyPlaceKind.First
            ? PositionOperation.BuyFirstPlace
            : PositionOperation.BuyPlace;

        var selection = await nextPosService.ResolveSelectionAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            operation,
            cancellationToken);

        if (selection is null)
            return Denied("no_available_position");

        var isClassic = selection.Algorithm.Equals(
            "classic",
            StringComparison.OrdinalIgnoreCase);
        var profileRoot = isClassic
            ? await placeQueries.GetFirstPlaceAsync(
                marketingAddr,
                structureNumber,
                profileAddr,
                cancellationToken)
            : null;
        var canSelectPosition = isClassic && profileRoot is not null;

        NextPosResponse? selectedPosition;

        if (requestedPosition is not null && isClassic)
        {
            if (requestedPosition.StructureNumber != structureNumber)
                return Denied("position_structure_mismatch");

            if (profileRoot is null)
                return Denied("profile_root_not_found_for_selected_position");

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
            if (!requestedMp.StartsWith(profileRoot.Mp, StringComparison.Ordinal))
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
                PosGroup = selection.Context.PosGroup,
                ProfileAddr = requestedParent.ProfileAddr,
                PlaceNumber = requestedParent.PlaceNumber,
                Pos = requestedPosition.Position
            };
        }
        else
        {
            // Radar and Chess always calculate their own position. A supplied
            // position is intentionally ignored for those algorithms.
            selectedPosition = await nextPosService.FindNextAsync(
                selection,
                cancellationToken);
        }

        if (selectedPosition is null)
            return Denied("no_available_position");

        if (selectedPosition.Pos == 0)
            return Denied("calculated_position_is_invalid");

        var parent = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            selectedPosition.ProfileAddr,
            selectedPosition.PlaceNumber,
            cancellationToken);
        if (parent is null)
            return Denied("parent_place_not_found");

        return new BuyPlaceDecision(
            CanBuy: true,
            command.Value.Kind,
            command.Value.Tag,
            IncludePosition: canSelectPosition,
            Position: selectedPosition,
            Reason: null)
        {
            RequireNextPosition = !canSelectPosition,
            ViewerRootMp = profileRoot?.Mp ?? selection.Context.Root.Mp,
            HasPlacesInBuyFirstPlaceStructures = hasPlacesInBuyFirstPlaceStructures,
            AvailableCommandTags = commandTags
        };
    }

    private static (BuyPlaceKind Kind, uint Tag)? SelectCommand(
        bool hasPlacesInBuyFirstPlaceStructures,
        IReadOnlySet<uint> availableTags)
    {
        if (!hasPlacesInBuyFirstPlaceStructures
            && availableTags.Contains(ProgramCommandTags.BuyFirstPlace))
        {
            return (BuyPlaceKind.First, ProgramCommandTags.BuyFirstPlace);
        }

        return availableTags.Contains(ProgramCommandTags.BuyPlace)
            ? (BuyPlaceKind.Regular, ProgramCommandTags.BuyPlace)
            : null;
    }

    private static BuyPlaceDecision Denied(string reason) =>
        new(false, null, null, false, null, reason);
}
