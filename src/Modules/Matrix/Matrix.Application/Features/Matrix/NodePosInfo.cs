namespace Matrix.Application.Features.Matrix;


/// <summary>
/// Same semantics as your original TreeInfo, but built around PlaceResponse + nextMp string + lock mp strings.
/// IMPORTANT: requires PlaceResponse.Filling to be populated (JsonIgnored).
/// </summary>
internal sealed record NodePosInfo(
    short Pos,
    bool IsRoot,
    bool IsLock,
    bool IsLocked,
    bool CanLock,
    bool IsNextPos,
    bool CanBuy);

internal sealed class TreeInfo
{
    private readonly string _rootMp;
    private readonly string _nextMp;
    private readonly string[] _lockMps;
    private readonly HashSet<string> _lockSet;

    public TreeInfo(PlaceResponse root, string nextMp, IEnumerable<string> lockMps)
    {
        _rootMp = root.Mp;
        _nextMp = nextMp;
        _lockMps = lockMps as string[] ?? lockMps.ToArray();
        _lockSet = _lockMps.ToHashSet();
    }

    public NodePosInfo GetNodePosInfo(PlaceResponse? parentRow, string mp)
    {
        var isRoot = mp == _rootMp;
        var isNextPos = mp == _nextMp;

        var lastChar = mp[^1];
        var pos = (short)(lastChar - '0');

        var mpButLast = mp[..^1];
        var siblingMp = mpButLast + (pos == 0 ? '1' : '0');
        
        var isLock = _lockSet.Contains(mp);

        // locked if any lock prefix matches
        var isLocked = _lockMps.Any(lockMp => mp.StartsWith(lockMp, StringComparison.Ordinal));
        
        var canBuy = 
            !isLocked 
            && mp.StartsWith(_rootMp, StringComparison.Ordinal)
            && parentRow is not null
            && pos == parentRow.Filling;
        
        var canLock =
            !isLocked
            && !_lockSet.Contains(siblingMp)
            && !isRoot
            && mp.StartsWith(_rootMp, StringComparison.Ordinal)
            && parentRow is not null
            && parentRow.Filling != 0;

        return new NodePosInfo(
            Pos: pos,
            IsRoot: isRoot,
            IsLock: isLock,
            IsLocked: isLocked,
            CanLock: canLock,
            IsNextPos: isNextPos,
            CanBuy: canBuy);
    }
}