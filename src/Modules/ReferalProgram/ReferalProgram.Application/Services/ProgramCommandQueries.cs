using Contracts.Application.Features.MarketingV3;
using MediatR;

namespace ReferalProgram.Application.Services;

public sealed class ProgramCommandQueries(ISender sender)
    : IProgramCommandQueries
{
    public async Task<ProgramCommandConfiguration> GetConfigurationAsync(
        string marketingAddr,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetMarketingDataQuery(marketingAddr),
            cancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"Could not load marketing command configuration: {string.Join(", ", result.Errors)}");

        var commandTagsByStructure = result.Value.Structures.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<uint>)entry.Value.Commands.Keys.ToHashSet());

        return new ProgramCommandConfiguration(commandTagsByStructure);
    }
}
