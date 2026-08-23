namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class TrimmedClassicPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries) : IPositionAlgorithmStrategy
{
    public const string AlgorithmName = "trimmed_classic";

    public string Name => AlgorithmName;

    public Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        if (context.CutFactor is null or < 2)
        {
            throw new InvalidOperationException(
                "trimmed_classic requires a cut_factor of at least 2.");
        }

        return ClassicPositionAlgorithmStrategy.FindClassicNextAsync(
            placeQueries,
            context,
            cancellationToken);
    }
}
