using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Application.Services.PositionStrategies;

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
    IProfileVolumeQueries profileVolumeQueries,
    IPositionAlgorithmConfigurationParser configurationParser,
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
        var referralVolumes = await profileVolumeQueries.GetReferralVolumesAsync(
            marketingAddr,
            number,
            retained.Select(place => place.ProfileAddr!).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        var inviters = await placeRepository.GetInvitersAsync(marketingAddr, cancellationToken);
        var positionLocks = await positionLockRepository.GetStructureLocksAsync(
            marketingAddr, number, cancellationToken);
        var configuration = configurationParser.Parse(structure.PosAlgo);

        var nodes = retained.ToDictionary(place => place.Id, place => new Node(place));
        var rootNode = nodes[root.Id];
        rootNode.PlaceAt(parent: null, RootMp, posGroup: 0, pos: 0);
        var posted = new List<Node> { rootNode };
        var memoryQueries = new InMemoryCompressionPositionCandidateQueries(posted);
        IPositionAlgorithmStrategy classicStrategy =
            new ClassicPositionAlgorithmStrategy(memoryQueries);
        IPositionAlgorithmStrategy emptyParentStrategy =
            new EmptyParentPositionAlgorithmStrategy(memoryQueries);
        var firstPostedByProfile = new Dictionary<string, Node>(StringComparer.Ordinal)
        {
            [root.ProfileAddr!] = rootNode
        };

        var ordered = retained
            .Where(place => place.Id != root.Id)
            .OrderByDescending(place => RankThreshold(place, ranks, referralVolumes))
            .ThenByDescending(place => referralVolumes.GetValueOrDefault(place.ProfileAddr!))
            .ThenBy(place => place.ActivatedAt ?? long.MaxValue)
            .ThenBy(place => place.Id)
            .ToArray();
        var remainingPlaces = ordered.Length;
        var useEmptyParentPositioning = false;

        foreach (var place in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var algorithmRoot = ResolveRoot(
                configuration.Root, place, rootNode, firstPostedByProfile, inviters);
            var strategy = useEmptyParentPositioning
                ? emptyParentStrategy
                : classicStrategy;
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
            var nextPosition = await strategy.FindNextAsync(
                new PositionAlgorithmStrategyContext(
                    marketingAddr,
                    number,
                    structure.Width,
                    algorithmRoot.ToResponse(),
                    PosGroup: 0,
                    ProfiledPlacesPrioritized: true,
                    DepthSpread: 1,
                    RootProfileLockMps: lockMps),
                cancellationToken);
            if (nextPosition is null)
                return $"The '{strategy.Name}' position algorithm found no position for place {place.Id}.";

            var parent = posted.Single(node =>
                node.Place.ProfileAddr == nextPosition.ProfileAddr
                && node.Place.PlaceNumber == nextPosition.PlaceNumber);

            var node = nodes[place.Id];
            node.PlaceAt(
                parent,
                nextPosition.Mp,
                nextPosition.PosGroup,
                nextPosition.Pos);
            posted.Add(node);
            if (place.PlaceNumber == 1)
                firstPostedByProfile.TryAdd(place.ProfileAddr!, node);

            remainingPlaces--;
            useEmptyParentPositioning |= ShouldUseEmptyParentPositioning(
                posted,
                node.Deep,
                structure.Width,
                remainingPlaces);
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

    private static uint RankThreshold(
        Place place,
        IEnumerable<StructureRankResponse> ranks,
        IReadOnlyDictionary<string, uint> referralVolumes) =>
        ranks.Where(rank => rank.RequiredActiveReferralPlaces
                <= referralVolumes.GetValueOrDefault(place.ProfileAddr!))
            .Select(rank => rank.RequiredActiveReferralPlaces)
            .DefaultIfEmpty(0u)
            .Max();

    private static bool ShouldUseEmptyParentPositioning(
        IReadOnlyCollection<Node> posted,
        uint filledDepth,
        byte width,
        int remainingPlaces)
    {
        if (remainingPlaces == 0 || width < 2)
            return false;

        long levelCapacity = 1;
        for (var depth = 1u; depth < filledDepth; depth++)
        {
            levelCapacity = checked(levelCapacity * width);
            if (levelCapacity > uint.MaxValue)
                return false;
        }

        var levelPlaces = posted.LongCount(node => node.Deep == filledDepth);
        return levelPlaces == levelCapacity && remainingPlaces < levelCapacity;
    }

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

    private sealed class Node(Place place)
    {
        public Place Place { get; } = place;
        public Node? Parent { get; private set; }
        public string Mp { get; private set; } = null!;
        public byte PosGroup { get; private set; }
        public uint Pos { get; private set; }
        public uint Filling { get; private set; }
        public uint Deep { get; private set; }

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
            }
        }

        public void Apply(long matrixFilling) =>
            Place.RebuildPosition(Parent?.Place, Mp, PosGroup, Pos, Filling, Deep, matrixFilling);

        public PlaceResponse ToResponse() => new()
        {
            Id = Place.Id,
            ParentId = Parent?.Place.Id,
            Mp = Mp,
            PosGroup = PosGroup,
            MarketingAddr = Place.MarketingAddr,
            StructNumber = Place.StructureNumber,
            ProfileAddr = Place.ProfileAddr,
            PlaceNumber = Place.PlaceNumber,
            ProfileLogin = Place.ProfileLogin,
            Kind = Place.Kind,
            Filling = Filling,
            Deep = Deep,
            IsActive = Place.IsActive,
            ActivatedAt = Place.ActivatedAt
        };
    }

    private sealed class InMemoryCompressionPositionCandidateQueries(
        IReadOnlyList<Node> posted) : IPositionCandidateQueries
    {
        public Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
            string marketingAddr,
            byte structureNumber,
            string mpPrefix,
            byte width,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var safePage = page > 0 ? page : 1;
            var safePageSize = pageSize > 0 ? pageSize : 50;
            IReadOnlyList<PlaceResponse> candidates = posted
                .Where(node => node.Place.MarketingAddr == marketingAddr
                    && node.Place.StructureNumber == structureNumber
                    && node.Mp.StartsWith(mpPrefix, StringComparison.Ordinal)
                    && node.Place.IsActive
                    && node.Place.Kind != PlaceKinds.TerminalClone
                    && (width == 0 || node.Filling < width))
                .OrderBy(node => node.Mp.Length)
                .ThenBy(node => node.Mp, StringComparer.Ordinal)
                .ThenBy(node => node.Place.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(node => node.ToResponse())
                .ToArray();
            return Task.FromResult(candidates);
        }

        public Task<PlaceResponse?> GetProfileFrontierCandidateAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            uint profiledFrontierLimit, IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PlaceResponse?> GetSystemGapCandidateAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            byte depthSpread, IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            bool profiledPlacesPrioritized, byte depthSpread,
            IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
