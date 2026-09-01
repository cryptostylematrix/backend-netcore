using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class StructureRankCalculatorTests
{
    [Theory]
    [InlineData(0, "Bronze")]
    [InlineData(4, "Bronze")]
    [InlineData(5, "Silver")]
    [InlineData(49, "Silver")]
    [InlineData(50, "Gold")]
    [InlineData(1000, "Diamond")]
    [InlineData(5000, "Diamond")]
    public void Resolves_highest_rank_achieved_by_referral_volume(
        uint referralVolume,
        string expected)
    {
        var result = StructureRankCalculator.Resolve(
            Ranks(),
            profileAddr: "profile",
            referralVolume);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Returns_null_when_no_threshold_is_achieved()
    {
        var ranks = new[] { Rank("Silver", 5) };

        var result = StructureRankCalculator.Resolve(
            ranks,
            profileAddr: "profile",
            referralVolume: 4);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_for_system_place_even_when_zero_threshold_exists()
    {
        var result = StructureRankCalculator.Resolve(
            Ranks(),
            profileAddr: null,
            referralVolume: 5000);

        Assert.Null(result);
    }

    private static StructureRankResponse[] Ranks() =>
    [
        Rank("Diamond", 1000),
        Rank("Gold", 50),
        Rank("Bronze", 0),
        Rank("Silver", 5)
    ];

    private static StructureRankResponse Rank(string name, uint threshold) => new()
    {
        MarketingAddr = "marketing",
        StructureNumber = 1,
        Name = name,
        RequiredActiveReferralPlaces = threshold
    };
}
