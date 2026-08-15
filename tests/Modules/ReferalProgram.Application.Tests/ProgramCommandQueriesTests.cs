using Ardalis.Result;
using Contracts.Application.Features.MarketingV3;
using Contracts.Dto;
using MediatR;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramCommandQueriesTests
{
    [Fact]
    public async Task Returns_command_tags_for_requested_structure()
    {
        var data = new MarketingV3DataResponse
        {
            Structures = new Dictionary<byte, MarketingV3StructureConfigResponse>
            {
                [2] = new()
                {
                    Commands = new Dictionary<uint, MarketingV3CommandConfigResponse>
                    {
                        [10] = new()
                    }
                },
                [4] = new()
                {
                    Commands = new Dictionary<uint, MarketingV3CommandConfigResponse>
                    {
                        [10] = new(),
                        [20] = new()
                    }
                }
            }
        };
        var sender = new SenderStub(Result.Success(data));
        var queries = new ProgramCommandQueries(sender);

        var configuration = await queries.GetConfigurationAsync("marketing", default);
        var result = configuration.GetAvailableCommandTags(4);

        Assert.Equal(new uint[] { 10, 20 }, result.Order());
        Assert.Equal(new byte[] { 2, 4 }, configuration.GetStructureNumbers(10).Order());
        Assert.Equal(new byte[] { 4 }, configuration.GetStructureNumbers(20).Order());
        Assert.Equal("marketing", sender.MarketingAddr);
    }

    [Fact]
    public async Task Returns_empty_set_when_structure_is_not_configured()
    {
        var queries = new ProgramCommandQueries(
            new SenderStub(Result.Success(new MarketingV3DataResponse())));

        var configuration = await queries.GetConfigurationAsync("marketing", default);
        var result = configuration.GetAvailableCommandTags(9);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Throws_with_contract_errors_when_loading_fails()
    {
        var queries = new ProgramCommandQueries(
            new SenderStub(Result<MarketingV3DataResponse>.Error("contract failed")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            queries.GetConfigurationAsync("marketing", default));

        Assert.Contains("contract failed", exception.Message);
    }

    private sealed class SenderStub(Result<MarketingV3DataResponse> result) : ISender
    {
        public string? MarketingAddr { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request is not GetMarketingDataQuery query)
                throw new NotSupportedException(request.GetType().Name);

            MarketingAddr = query.MarketingAddr;
            return Task.FromResult((TResponse)(object)result);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
