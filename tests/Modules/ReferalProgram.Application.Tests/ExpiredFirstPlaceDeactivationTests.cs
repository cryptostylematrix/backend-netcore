using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ExpiredFirstPlaceDeactivationTests
{
    [Fact]
    public async Task Uses_calendar_month_cutoff_and_requested_structure()
    {
        var repository = new Repository();
        await new ExpiredFirstPlaceDeactivationService(repository, new Structures()).ExecuteAsync(Request(), default);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(), repository.Cutoff);
    }

    [Fact]
    public async Task Cancellation_prevents_writes()
    {
        var repository = new Repository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ExpiredFirstPlaceDeactivationService(repository, new Structures()).ExecuteAsync(Request(), cancellation.Token));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Invalid_requests_do_not_write()
    {
        var repository = new Repository();
        var service = new ExpiredFirstPlaceDeactivationService(repository, new Structures());
        foreach (var request in new[] { Request() with { StructureNumber = -1 }, Request() with { StructureNumber = 256 },
            Request() with { MarketingAddress = "" }, Request() with { Period = new("months", 0) },
            Request() with { OccurredOnUtc = DateTime.SpecifyKind(Request().OccurredOnUtc, DateTimeKind.Unspecified) } })
            await Assert.ThrowsAsync<FormatException>(() => service.ExecuteAsync(request, default));
        await Assert.ThrowsAsync<FormatException>(() => new ExpiredFirstPlaceDeactivationService(repository, new Structures(false)).ExecuteAsync(Request(), default));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Consumer_acknowledges_success_and_reports_invalid_requests()
    {
        var services = new ServiceCollection();
        var repository = new Repository();
        services.AddSingleton<IPlaceRepository>(repository);
        services.AddSingleton<IStructureQueries>(new Structures());
        services.AddSingleton<ExpiredFirstPlaceDeactivationService>();
        services.AddMessageBroker(registration => registration.AddConsumer<DeactivateExpiredFirstPlacesRequestConsumer>());
        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>().CreateRequestClient<DeactivateExpiredFirstPlacesRequest>();
            Assert.Null((await client.GetResponse<IntegrationRequestResponse>(Request())).Message.Errors);
            Assert.NotEmpty((await client.GetResponse<IntegrationRequestResponse>(Request() with { StructureNumber = 256 })).Message.Errors!);
            Assert.Equal(1, repository.Calls);
        }
        finally { await bus.StopAsync(); }
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public int Calls { get; private set; }
        public long Cutoff { get; private set; }
        public override Task<int> ExpireFirstPlacesAsync(string marketingAddr, byte structureNumber, long cutoffUtc, CancellationToken cancellationToken)
        {
            Assert.Equal("marketing", marketingAddr);
            Assert.Equal((byte)0, structureNumber);
            Calls++;
            Cutoff = cutoffUtc;
            return Task.FromResult(2);
        }
    }
    private sealed class Structures(bool exists = true) : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) =>
            Task.FromResult<StructureResponse?>(exists ? new() : null);
    }
    private static DeactivateExpiredFirstPlacesRequest Request() => new("marketing", 0,
        new("months", 1), Guid.NewGuid(), new DateTime(2026, 3, 31, 12, 0, 0, DateTimeKind.Utc));
}
