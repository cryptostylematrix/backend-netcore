using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.LockAggregate;

namespace ReferalProgram.Application.Services;

public interface IStructureCompressionService
{
    Task<string?> CompressAsync(
        string marketingAddr,
        int structureNumber,
        CancellationToken cancellationToken);
}

public sealed class StructureCompressionService(
    IPlaceRepository placeRepository,
    IPositionLockRepository positionLockRepository,
    IStructureQueries structureQueries,
    IStructureRankQueries rankQueries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionGroupSelector groupSelector,
    IProgramUnitOfWork unitOfWork) : IStructureCompressionService
{
    private const string RootMp = "00000000";

    public async Task<string?> CompressAsync(
        string marketingAddr,
        int structureNumber,
        CancellationToken cancellationToken)
    {
        if (structureNumber is < byte.MinValue or > byte.MaxValue)
            return $"Structure number {structureNumber} is outside the byte range.";

        var number = checked((byte)structureNumber);
        var structure = await structureQueries.GetStructureAsync(
            marketingAddr, number, cancellationToken);
        if (structure is null)
            return $"Structure {structureNumber} for Referral Program '{marketingAddr}' was not found.";

        var places = await placeRepository.GetStructurePlacesAsync(
            marketingAddr, number, cancellationToken);
        var root = places.FirstOrDefault(place => place.ParentId is null && place.PlaceNumber == 1);
        if (root is null || !root.IsActive || string.IsNullOrWhiteSpace(root.ProfileAddr))
            return "Structure compression requires an active profiled root place.";

        var retained = places
            .Where(place => place.IsActive && !string.IsNullOrWhiteSpace(place.ProfileAddr))
            .ToArray();
        var removed = places.Except(retained).ToArray();
        var ranks = await rankQueries.GetAllAsync(marketingAddr, number, cancellationToken);
        var inviters = await placeRepository.GetInvitersAsync(marketingAddr, cancellationToken);
        var positionLocks = await positionLockRepository.GetStructureLocksAsync(
            marketingAddr, number, cancellationToken);
        var configuration = configurationParser.Parse(structure.PosAlgo);

        var nodes = retained.ToDictionary(place => place.Id, place => new Node(place));
        var rootNode = nodes[root.Id];
        rootNode.PlaceAt(parent: null, RootMp, posGroup: 0, pos: 0);
        var posted = new List<Node> { rootNode };
        var firstPostedByProfile = new Dictionary<string, Node>(StringComparer.Ordinal)
        {
            [root.ProfileAddr!] = rootNode
        };

        var ordered = retained
            .Where(place => place.Id != root.Id)
            .OrderByDescending(place => RankThreshold(place, ranks))
            .ThenBy(place => place.ActivatedAt ?? long.MaxValue)
            .ThenBy(place => place.Id);

        foreach (var place in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var algorithmRoot = ResolveRoot(
                configuration.Root, place, rootNode, firstPostedByProfile, inviters);
            var counts = posted
                .GroupBy(node => node.PosGroup)
                .ToDictionary(group => group.Key, group => (long)group.Count());
            var group = groupSelector.Select(configuration, counts);
            var lockMps = positionLocks
                .Where(positionLock => positionLock.ProfileAddr == algorithmRoot.Place.ProfileAddr)
                .Select(positionLock =>
                {
                    var lockPlace = posted.FirstOrDefault(node =>
                        node.Place.ProfileAddr == positionLock.PlaceProfileAddr
                        && node.Place.PlaceNumber == positionLock.PlaceNumber);
                    return lockPlace is null
                        ? null
                        : lockPlace.Mp + positionLock.LockedPos.ToString("X8");
                })
                .Where(mp => mp is not null)
                .Cast<string>()
                .ToArray();
            var parent = FindParent(group, algorithmRoot, posted, structure.Width, lockMps);
            if (parent is null)
                return $"The '{group.Algorithm}' position algorithm found no position for place {place.Id}.";

            var pos = checked(parent.Filling + 1);
            var node = nodes[place.Id];
            node.PlaceAt(
                parent,
                parent.Mp + pos.ToString("X8"),
                checked((byte)group.Id),
                pos);
            posted.Add(node);
            if (place.PlaceNumber == 1)
                firstPostedByProfile.TryAdd(place.ProfileAddr!, node);
        }

        foreach (var node in posted)
        {
            var matrixFilling = posted.LongCount(candidate =>
                candidate.Mp.StartsWith(node.Mp, StringComparison.Ordinal)
                && candidate.Deep <= checked(node.Deep + structure.Height));
            node.Apply(matrixFilling);
        }

        var postedByPlace = posted.ToDictionary(
            node => (node.Place.ProfileAddr!, node.Place.PlaceNumber));
        var obsoleteLocks = new List<PositionLock>();
        foreach (var positionLock in positionLocks)
        {
            if (!postedByPlace.TryGetValue(
                    (positionLock.PlaceProfileAddr, positionLock.PlaceNumber),
                    out var lockPlace))
            {
                obsoleteLocks.Add(positionLock);
                continue;
            }
            positionLock.RebuildMp(lockPlace.Mp + positionLock.LockedPos.ToString("X8"));
        }
        positionLockRepository.RemoveRange(obsoleteLocks);

        await placeRepository.RemoveRangeAsync(removed, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return null;
    }

    private static uint RankThreshold(Place place, IEnumerable<StructureRankResponse> ranks) =>
        ranks.Where(rank => rank.RequiredActiveReferralPlaces <= place.PersonalVolume)
            .Select(rank => rank.RequiredActiveReferralPlaces)
            .DefaultIfEmpty(0u)
            .Max();

    private static Node ResolveRoot(
        string rootStrategy,
        Place place,
        Node ownerRoot,
        IReadOnlyDictionary<string, Node> firstPostedByProfile,
        IReadOnlyDictionary<string, string?> inviters)
    {
        if (rootStrategy.Equals("owner", StringComparison.OrdinalIgnoreCase))
            return ownerRoot;
        if (!rootStrategy.Equals("profile", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unknown root strategy '{rootStrategy}'.");

        var profile = place.ProfileAddr;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (profile is not null && visited.Add(profile))
        {
            if (firstPostedByProfile.TryGetValue(profile, out var root))
                return root;
            profile = inviters.GetValueOrDefault(profile);
        }
        return ownerRoot;
    }

    private static Node? FindParent(
        PositionGroupConfiguration group,
        Node root,
        IReadOnlyList<Node> posted,
        byte width,
        IReadOnlyCollection<string> lockMps)
    {
        var candidates = posted.Where(node =>
                node.Mp.StartsWith(root.Mp, StringComparison.Ordinal)
                && node.Place.Kind != PlaceKinds.TerminalClone
                && (width == 0 || node.Filling < width)
                && !lockMps.Any(lockMp =>
                    (node.Mp + checked(node.Filling + 1).ToString("X8"))
                        .StartsWith(lockMp, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (candidates.Length == 0)
            return null;

        return group.Algorithm.ToLowerInvariant() switch
        {
            "classic" or "trimmed_classic" => candidates
                .OrderBy(node => node.Mp.Length).ThenBy(node => node.Mp).ThenBy(node => node.Place.Id)
                .First(),
            "radar" => Radar(candidates, group),
            "chess" => Chess(candidates, group, width),
            "system_gap" => candidates
                .Where(node => !(node.ProfiledChildren == 0 && width > 0 && node.Filling + 1 >= width))
                .OrderBy(node => node.Deep).ThenBy(node => node.Mp).ThenBy(node => node.Place.Id)
                .FirstOrDefault(),
            "profile_frontier" => ProfileFrontier(candidates, root, posted, group),
            _ => throw new InvalidOperationException(
                $"Unknown position algorithm '{group.Algorithm}'.")
        };
    }

    private static Node? Radar(Node[] candidates, PositionGroupConfiguration group)
    {
        var minDepth = candidates.Min(node => node.Deep);
        return candidates
            .Where(node => node.Deep < minDepth + group.DepthSpread)
            .OrderBy(node => node.Filling)
            .ThenBy(node => node.Place.ActivatedAt ?? long.MaxValue)
            .ThenBy(node => node.Deep).ThenBy(node => node.Mp).ThenBy(node => node.Place.Id)
            .FirstOrDefault();
    }

    private static Node? Chess(Node[] candidates, PositionGroupConfiguration group, byte width)
    {
        var minDepth = candidates.Min(node => node.Deep);
        var window = candidates.Where(node => node.Deep < minDepth + group.DepthSpread)
            .OrderBy(node => node.Deep).ThenBy(node => node.Mp).ThenBy(node => node.Place.Id)
            .ToArray();
        var chess = new List<Node>(window.Length);
        for (var left = 0; left < window.Length; left++)
        {
            var right = window.Length - 1 - left;
            if (left > right) break;
            chess.Add(window[left]);
            if (left != right) chess.Add(window[right]);
        }
        for (uint filling = 0; filling < width; filling++)
        {
            var candidate = chess.FirstOrDefault(node => node.Filling == filling);
            if (candidate is not null) return candidate;
        }
        return null;
    }

    private static Node? ProfileFrontier(
        Node[] candidates,
        Node root,
        IReadOnlyList<Node> posted,
        PositionGroupConfiguration group)
    {
        if (group.ProfiledFrontierLimit is null or 0)
            throw new InvalidOperationException("profile_frontier requires a positive profiled_frontier_limit.");
        var frontier = posted.Count(node =>
            node.Mp.StartsWith(root.Mp, StringComparison.Ordinal) && node.ProfiledChildren == 0);
        var eligible = frontier < group.ProfiledFrontierLimit
            ? candidates
            : candidates.Where(node => node.ProfiledChildren == 0).ToArray();
        return eligible.OrderBy(node => node.Deep)
            .ThenBy(node => node.ProfiledChildren).ThenBy(node => node.Mp).ThenBy(node => node.Place.Id)
            .FirstOrDefault();
    }

    private sealed class Node(Place place)
    {
        public Place Place { get; } = place;
        public Node? Parent { get; private set; }
        public string Mp { get; private set; } = null!;
        public byte PosGroup { get; private set; }
        public uint Pos { get; private set; }
        public uint Filling { get; private set; }
        public uint Deep { get; private set; }
        public uint ProfiledChildren { get; private set; }

        public void PlaceAt(Node? parent, string mp, byte posGroup, uint pos)
        {
            Parent = parent;
            Mp = mp;
            PosGroup = posGroup;
            Pos = pos;
            Deep = parent is null ? 1u : checked(parent.Deep + 1);
            if (parent is not null)
            {
                parent.Filling = checked(parent.Filling + 1);
                parent.ProfiledChildren = checked(parent.ProfiledChildren + 1);
            }
        }

        public void Apply(long matrixFilling) =>
            Place.RebuildPosition(Parent?.Place, Mp, PosGroup, Pos, Filling, Deep, matrixFilling);
    }
}
