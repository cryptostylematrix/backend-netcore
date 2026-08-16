using Contracts.Dto;

namespace ProgramMigrator;

internal sealed class InviteTreeLoader(
    ContractApiClient contracts,
    uint programId,
    int maxInvites,
    IMigrationProgress progress)
{
    public async Task<(string RootProfileAddr, string RootInviteAddr, IReadOnlyList<PlaceMigrationNode> Nodes)>
        LoadAsync(
            string? rootProfileAddr,
            string? rootProfileLogin,
            CancellationToken cancellationToken)
    {
        rootProfileAddr ??= (await contracts.GetProfileAddressByLoginAsync(
            rootProfileLogin!,
            cancellationToken)).Addr;

        var program = await GetProgramAsync(rootProfileAddr, cancellationToken);
        if (program.Confirmed != 1)
            throw new InvalidOperationException("The root profile program is not confirmed.");

        var rootInviteAddr = Required(program.InviteAddr, "Root Invite address");
        var queue = new List<PendingInvite>
        {
            new(rootInviteAddr, null, Pos: 0, Mp: "00000000", Deep: 1)
        };
        var seenInviteAddrs = new HashSet<string>(StringComparer.Ordinal)
        {
            rootInviteAddr
        };
        var seenProfileAddrs = new HashSet<string>(StringComparer.Ordinal);
        var nodes = new List<PlaceMigrationNode>();

        for (var cursor = 0; cursor < queue.Count; cursor++)
        {
            var pending = queue[cursor];
            progress.Report("Invite contracts", cursor + 1, queue.Count);

            var invite = await contracts.GetInviteDataAsync(
                pending.InviteAddr,
                cancellationToken);

            if (unchecked((uint)invite.Program) != programId)
            {
                throw new InvalidOperationException(
                    $"Invite {pending.InviteAddr} belongs to program "
                    + $"{unchecked((uint)invite.Program):X8}, expected {programId:X8}.");
            }

            var owner = invite.Owner
                ?? throw new InvalidOperationException(
                    $"Invite {pending.InviteAddr} has no owner.");
            var profileAddr = Required(owner.OwnerAddr, "Invite owner address");

            if (!seenProfileAddrs.Add(profileAddr))
            {
                throw new InvalidOperationException(
                    $"Profile {profileAddr} occurs more than once in the Invite tree.");
            }

            if (invite.NextRefNo < 1)
            {
                throw new InvalidOperationException(
                    $"Invite {pending.InviteAddr} returned invalid next_ref_no {invite.NextRefNo}.");
            }

            if (pending.ParentInviteAddr is not null
                && !string.Equals(
                    invite.ParentAddr,
                    pending.ParentInviteAddr,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Invite {pending.InviteAddr} has unexpected parent {invite.ParentAddr}.");
            }

            var profile = await contracts.GetProfileDataAsync(profileAddr, cancellationToken);
            var profileLogin = Required(profile.Content?.Login, "Profile login");

            nodes.Add(new PlaceMigrationNode(
                pending.InviteAddr,
                pending.ParentInviteAddr,
                pending.InviteAddr,
                StructureNumber: 0,
                profileAddr,
                profileLogin,
                PlaceNumber: 1,
                owner.SetAt,
                Kind: 0,
                pending.Pos,
                checked((uint)(invite.NextRefNo - 1)),
                pending.Deep,
                pending.Mp));

            for (var refNo = 1; refNo < invite.NextRefNo; refNo++)
            {
                var child = await contracts.GetInviteAddressAsync(
                    pending.InviteAddr,
                    checked((uint)refNo),
                    cancellationToken);
                var childAddr = Required(child.Addr, "Child Invite address");

                if (!seenInviteAddrs.Add(childAddr))
                {
                    throw new InvalidOperationException(
                        $"Invite cycle or duplicate detected at {childAddr}.");
                }

                if (seenInviteAddrs.Count > maxInvites)
                {
                    throw new InvalidOperationException(
                        $"Invite tree exceeds the configured limit of {maxInvites} places.");
                }

                queue.Add(new PendingInvite(
                    childAddr,
                    pending.InviteAddr,
                    checked((uint)refNo),
                    pending.Mp + refNo.ToString("X8"),
                    checked(pending.Deep + 1)));
            }
        }

        return (rootProfileAddr, rootInviteAddr, nodes);
    }

    private async Task<ProgramDataResponse> GetProgramAsync(
        string rootProfileAddr,
        CancellationToken cancellationToken)
    {
        var programs = await contracts.GetProfileProgramsAsync(
            rootProfileAddr,
            cancellationToken);
        var expectedKey = programId.ToString("X");

        foreach (var item in programs)
        {
            foreach (var (key, value) in item)
            {
                var normalizedKey = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? key[2..]
                    : key;

                if (normalizedKey.Equals(expectedKey, StringComparison.OrdinalIgnoreCase))
                    return value;
            }
        }

        throw new InvalidOperationException(
            $"Program {programId:X8} was not found on root Profile {rootProfileAddr}.");
    }

    private static string Required(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{fieldName} is empty.")
            : value.Trim();
}
