namespace ReferalProgram.Application.Services;

public sealed class ProfileRootPlaceResolver(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries)
    : IProfileRootPlaceResolver
{
    private const byte InviteStructureNumber = 0;
    private const uint FirstPlaceNumber = 1;

    public async Task<PlaceResponse?> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        var currentProfileAddr = string.IsNullOrWhiteSpace(profileAddr)
            ? null
            : profileAddr;
        bool? allowInactiveInviter = null;
        byte? activityStructure = null;
        var activityCache = new Dictionary<string, bool>(StringComparer.Ordinal);
        var visitedProfileAddrs = new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            if (currentProfileAddr is not null
                && !visitedProfileAddrs.Add(currentProfileAddr))
            {
                return null;
            }

            var root = await placeQueries.GetPlaceAsync(
                marketingAddr,
                structureNumber,
                currentProfileAddr,
                FirstPlaceNumber,
                cancellationToken);

            if (root is not null)
                return root;

            if (currentProfileAddr is null)
                return null;

            var invite = await placeQueries.GetPlaceAsync(
                marketingAddr,
                InviteStructureNumber,
                currentProfileAddr,
                FirstPlaceNumber,
                cancellationToken);

            if (invite is null)
                return null;

            if (allowInactiveInviter is null)
            {
                var structure = await structureQueries.GetStructureAsync(
                    marketingAddr, InviteStructureNumber, cancellationToken);
                activityStructure = await ActivitySourceResolver.ResolveAsync(structure, placeQueries, cancellationToken);
                allowInactiveInviter = structure?.Activity is { } activity
                    && ((InviteActivitySettings)ActivitySettings.Parse(activity, InviteStructureNumber))
                        .WhenInactive.AllowAsFallbackRoot;
            }

            var inviter = await FindFirstEligibleInviterAsync(
                invite, allowInactiveInviter.Value, activityStructure, activityCache, cancellationToken);
            if (inviter?.ProfileAddr is not { } inviterProfileAddr
                || string.IsNullOrWhiteSpace(inviterProfileAddr))
            {
                return null;
            }

            currentProfileAddr = inviterProfileAddr;
        }
    }

    private async Task<PlaceResponse?> FindFirstEligibleInviterAsync(
        PlaceResponse invite,
        bool allowInactiveInviter,
        byte? activityStructure,
        IDictionary<string, bool> activityCache,
        CancellationToken cancellationToken)
    {
        var parentId = invite.ParentId;
        var visitedInviteIds = new HashSet<int> { invite.Id };

        while (parentId is not null)
        {
            var inviter = await placeQueries.GetPlaceAsync(
                parentId.Value,
                cancellationToken);

            if (inviter is null || !visitedInviteIds.Add(inviter.Id))
                return null;

            if ((allowInactiveInviter || await ActivitySourceResolver.IsActiveAsync(inviter, activityStructure, placeQueries, activityCache, cancellationToken))
                && !string.IsNullOrWhiteSpace(inviter.ProfileAddr))
            {
                return inviter;
            }

            parentId = inviter.ParentId;
        }

        return null;
    }
}
