namespace ReferalProgram.Application.Services;

public sealed class ProfileRootPlaceResolver(IPlaceQueries placeQueries)
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

            var inviter = await FindFirstActiveInviterAsync(invite, cancellationToken);
            if (inviter?.ProfileAddr is not { } inviterProfileAddr
                || string.IsNullOrWhiteSpace(inviterProfileAddr))
            {
                return null;
            }

            currentProfileAddr = inviterProfileAddr;
        }
    }

    private async Task<PlaceResponse?> FindFirstActiveInviterAsync(
        PlaceResponse invite,
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

            if (inviter.IsActive
                && !string.IsNullOrWhiteSpace(inviter.ProfileAddr))
            {
                return inviter;
            }

            parentId = inviter.ParentId;
        }

        return null;
    }
}
