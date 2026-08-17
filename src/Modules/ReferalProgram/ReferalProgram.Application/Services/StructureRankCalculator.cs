namespace ReferalProgram.Application.Services;

internal static class StructureRankCalculator
{
    public static string? Resolve(
        IEnumerable<StructureRankResponse> ranks,
        uint personalVolume) =>
        ranks
            .Where(rank => rank.RequiredActiveReferralPlaces <= personalVolume)
            .OrderByDescending(rank => rank.RequiredActiveReferralPlaces)
            .Select(rank => rank.Name)
            .FirstOrDefault();
}
