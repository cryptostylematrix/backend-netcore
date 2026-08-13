using Contracts.Application.Features.MarketingV3;
using MediatR;

namespace ReferalProgram.Application.Services;

public sealed class ProgramCommandQueries(ISender sender)
    : IProgramCommandQueries
{
    public async Task<IReadOnlySet<uint>> GetAvailableCommandTagsAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetMarketingDataQuery(marketingAddr),
            cancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"Could not load marketing command configuration: {string.Join(", ", result.Errors)}");

        return result.Value.Structures.TryGetValue(structureNumber, out var structure)
            ? structure.Commands.Keys.ToHashSet()
            : new HashSet<uint>();
    }
}
