using Ardalis.Result;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Statistics;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class GetProgramStatisticsQueryHandlerTests
{
    [Fact]
    public async Task Returns_statistics_and_normalizes_addresses()
    {
        var expected = new ProgramStatisticsResponse
        {
            MarketingAddr = "marketing",
            ProfileAddr = "profile",
            Referrals = new ReferralCountStatisticsResponse(),
            Structures = []
        };
        var queries = new StatisticsQueriesStub(expected);
        var handler = new GetProgramStatisticsQueryHandler(queries);

        var result = await handler.Handle(
            new GetProgramStatisticsQuery("  marketing  ", "  profile  "),
            default);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
        Assert.Equal("marketing", queries.MarketingAddr);
        Assert.Equal("profile", queries.ProfileAddr);
    }

    [Fact]
    public async Task Returns_not_found_when_profile_is_not_in_program()
    {
        var handler = new GetProgramStatisticsQueryHandler(
            new StatisticsQueriesStub(null));

        var result = await handler.Handle(
            new GetProgramStatisticsQuery("marketing", "unknown-profile"),
            default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("", "profile")]
    [InlineData("marketing", "  ")]
    public async Task Rejects_blank_required_addresses(
        string marketingAddr,
        string profileAddr)
    {
        var queries = new StatisticsQueriesStub(null);
        var handler = new GetProgramStatisticsQueryHandler(queries);

        var result = await handler.Handle(
            new GetProgramStatisticsQuery(marketingAddr, profileAddr),
            default);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Null(queries.MarketingAddr);
    }

    private sealed class StatisticsQueriesStub(ProgramStatisticsResponse? response)
        : IProgramStatisticsQueries
    {
        public string? MarketingAddr { get; private set; }
        public string? ProfileAddr { get; private set; }

        public Task<ProgramStatisticsResponse?> GetAsync(
            string marketingAddr,
            string profileAddr,
            CancellationToken cancellationToken)
        {
            MarketingAddr = marketingAddr;
            ProfileAddr = profileAddr;
            return Task.FromResult(response);
        }
    }
}
