using ReferalProgram.Application.Services.PositionStrategies;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Policies;

public sealed class ClonePlaceKindPolicy(IPlaceRepository placeRepository)
    : IClonePlaceKindPolicy
{
    public async Task<byte> ResolveAsync(
        PositionSelection selection,
        int parentId,
        CancellationToken cancellationToken)
    {
        if (!selection.Algorithm.Equals(
                TrimmedClassicPositionAlgorithmStrategy.AlgorithmName,
                StringComparison.OrdinalIgnoreCase))
        {
            return PlaceKinds.Clone;
        }

        var cutFactor = selection.Context.CutFactor;
        if (cutFactor is null or < 2)
        {
            throw new InvalidOperationException(
                "trimmed_classic requires a cut_factor of at least 2.");
        }

        var existingCloneChildren = await placeRepository.CountCloneChildrenAsync(
            parentId,
            cancellationToken);
        var cloneOrdinal = checked(existingCloneChildren + 1);

        return cloneOrdinal % cutFactor.Value == 0
            ? PlaceKinds.TerminalClone
            : PlaceKinds.Clone;
    }
}
