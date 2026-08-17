namespace ReferalProgram.Application.Services;

internal static class StructureRankCalculator
{
    public static string? Resolve(
        IEnumerable<StructureRankResponse> ranks,
        string? profileAddr,
        uint personalVolume)
    {
        if (string.IsNullOrWhiteSpace(profileAddr))
            return null;

        return ranks
            .Where(rank => rank.RequiredActiveReferralPlaces <= personalVolume)
            .OrderByDescending(rank => rank.RequiredActiveReferralPlaces)
            .Select(rank => rank.Name)
            .FirstOrDefault();
    }
}
