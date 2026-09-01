namespace ReferalProgram.Application.Services;

internal static class StructureRankCalculator
{
    public static string? Resolve(
        IEnumerable<StructureRankResponse> ranks,
        string? profileAddr,
        uint referralVolume)
    {
        if (string.IsNullOrWhiteSpace(profileAddr))
            return null;

        return ranks
            .Where(rank => rank.RequiredActiveReferralPlaces <= referralVolume)
            .OrderByDescending(rank => rank.RequiredActiveReferralPlaces)
            .Select(rank => rank.Name)
            .FirstOrDefault();
    }
}
