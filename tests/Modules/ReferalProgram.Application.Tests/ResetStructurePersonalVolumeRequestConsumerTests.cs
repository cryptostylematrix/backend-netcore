using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class ResetStructurePersonalVolumeRequestConsumerTests
{
    [Fact]
    public async Task Resets_personal_volume_for_every_place_in_the_requested_structure()
    {
        var places = new[]
        {
            CreatePlace("first", personalVolume: 3),
            CreatePlace("second", personalVolume: 0),
            CreatePlace("third", personalVolume: 12)
        };
        var repository = new StubPlaceRepository(places);
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IPlaceRepository>(repository);
        services.AddSingleton<IProgramUnitOfWork>(unitOfWork);
        services.AddMessageBroker(registration =>
            registration.AddConsumer<ResetStructurePersonalVolumeRequestConsumer>());

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<ResetStructurePersonalVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new ResetStructurePersonalVolumeRequest(
                    "marketing",
                    2,
                    Guid.NewGuid(),
                    DateTime.UtcNow));

            Assert.Null(response.Message.Errors);
            Assert.Equal("marketing", repository.MarketingAddress);
            Assert.Equal((byte)2, repository.StructureNumber);
            Assert.All(places, place => Assert.Equal<uint>(0, place.PersonalVolume));
            Assert.Equal(1, unitOfWork.SaveCount);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public async Task Rejects_structure_numbers_outside_the_byte_range(int structureNumber)
    {
        var repository = new StubPlaceRepository([]);
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IPlaceRepository>(repository);
        services.AddSingleton<IProgramUnitOfWork>(unitOfWork);
        services.AddMessageBroker(registration =>
            registration.AddConsumer<ResetStructurePersonalVolumeRequestConsumer>());

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<ResetStructurePersonalVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new ResetStructurePersonalVolumeRequest(
                    "marketing",
                    structureNumber,
                    Guid.NewGuid(),
                    DateTime.UtcNow));

            Assert.NotNull(response.Message.Errors);
            Assert.Single(response.Message.Errors);
            Assert.Equal(0, repository.LookupCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static Place CreatePlace(string profile, uint personalVolume) =>
        Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 2,
            profileAddr: profile,
            profileLogin: profile,
            index: $"{profile}1",
            placeNumber: 1,
            parentProfileAddr: "parent",
            parentProfileLogin: "parent",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: true,
            createdAt: 1,
            activatedAt: null,
            personalVolume,
            groupVolume: 0);

    private sealed class StubPlaceRepository(IReadOnlyList<Place> places)
        : PlaceRepositoryStub
    {
        public int LookupCount { get; private set; }
        public string? MarketingAddress { get; private set; }
        public byte? StructureNumber { get; private set; }

        public override Task<IReadOnlyList<Place>> GetStructurePlacesAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken)
        {
            LookupCount++;
            MarketingAddress = marketingAddr;
            StructureNumber = structureNumber;
            return Task.FromResult(places);
        }
    }

    private sealed class StubProgramUnitOfWork : IProgramUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }
}
