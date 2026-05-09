namespace Marketing.Application.Features.Matrix;

/// <summary>
/// Same semantics as your original TreeInfo, but built around PlaceResponse + nextMp string + lock mp strings.
/// IMPORTANT: requires PlaceResponse.Filling to be populated (JsonIgnored).
/// </summary>
internal sealed record NodePosInfo(
    uint Pos,
    bool IsRoot,
    bool IsLock,
    bool IsLocked,
    bool CanLock,
    bool IsNextPos,
    bool CanBuy);

internal sealed class TreeInfo
{
    private readonly uint _width;
    private readonly string _rootMp;
    private readonly string _nextMp;
    private readonly string[] _lockMps;
    private readonly HashSet<string> _lockSet;

    public TreeInfo(PlaceResponse root, string nextMp, IEnumerable<string> lockMps)
    {
        _width = root.Width;
        _rootMp = root.Mp;
        _nextMp = nextMp;
        _lockMps = lockMps as string[] ?? lockMps.ToArray();
        _lockSet = _lockMps.ToHashSet();
    }

    public NodePosInfo GetNodePosInfo(PlaceResponse? parentRow, string mp)
    {
        var isRoot = mp == _rootMp;
        var isNextPos = mp == _nextMp;
        
        var posHex = mp[^8..];
        var pos = Convert.ToUInt32(posHex, 16);

        var parentMp = mp[..^8];

        // var mpButLast = mp[..^1];
        // var siblingMp = mpButLast + (pos == 0 ? '1' : '0');
        
        var isLock = _lockSet.Contains(mp);
        var availableSlots = uint.MaxValue;
        if (_width > 0)
        {
            availableSlots = _width;
            for (var childPos = 1; childPos <= _width; childPos++)
            {
                var childMp = parentMp + childPos.ToString("X8");
                if (_lockSet.Contains(childMp))
                {
                    availableSlots -= 1;
                }
            }
          
        }

        // locked if any lock prefix matches
        var isLocked = _lockMps.Any(lockMp => mp.StartsWith(lockMp, StringComparison.Ordinal));
        
        var canBuy = 
            !isLocked 
            && mp.StartsWith(_rootMp, StringComparison.Ordinal) // inside the root's subtree
            && parentRow is not null
            && parentRow.SeqNo > 0          // parent is not an empty slot 
            && pos == parentRow.SeqNo + 1;  // first available slot
        
        var canLock =
            !isLocked
            && availableSlots > 1           // if the pos is not the only available unlocked pos
            && !isRoot
            && mp.StartsWith(_rootMp, StringComparison.Ordinal)
            && parentRow is not null
            && parentRow.SeqNo > 0; // parent is not an empty slot 

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