using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class CalculateStructurePersonalVolumeRequestConsumerTests
{
    [Fact]
    public async Task Sets_first_place_volume_to_active_paid_places_of_direct_referrals()
    {
        var inviterFirst = CreatePlace("inviter", 1, isActive: true, personalVolume: 99);
        var inviterSecond = CreatePlace("inviter", 2, isActive: true, personalVolume: 99);
        var referralOneFirst = CreatePlace("referral-one", 1, isActive: true, personalVolume: 8);
        var referralOneSecond = CreatePlace("referral-one", 2, isActive: true, personalVolume: 8);
        var referralTwoFirst = CreatePlace("referral-two", 1, isActive: true, personalVolume: 8);
        var inactiveReferralPlace = CreatePlace(
            "referral-two", 2, isActive: false, personalVolume: 8);
        var unrelatedFirst = CreatePlace("unrelated", 1, isActive: true, personalVolume: 8);
        var systemPlace = CreatePlace(null, 1, isActive: true, personalVolume: 8);
        var places = new[]
        {
            inviterFirst,
            inviterSecond,
            referralOneFirst,
            referralOneSecond,
            referralTwoFirst,
            inactiveReferralPlace,
            unrelatedFirst,
            systemPlace
        };
        var repository = new StubPlaceRepository(
            places,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["referral-one"] = "inviter",
                ["referral-two"] = "inviter",
                ["unrelated"] = "another-inviter"
            });
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IPlaceRepository>(repository);
        services.AddSingleton<IProgramUnitOfWork>(unitOfWork);
        services.AddMessageBroker(registration =>
            registration.AddConsumer<CalculateStructurePersonalVolumeRequestConsumer>());

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<CalculateStructurePersonalVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new CalculateStructurePersonalVolumeRequest(
                    "marketing",
                    2,
                    Guid.NewGuid(),
                    DateTime.UtcNow));

            Assert.Null(response.Message.Errors);
            Assert.Equal<uint>(3, inviterFirst.PersonalVolume);
            Assert.All(
                places.Where(place => place != inviterFirst),
                place => Assert.Equal<uint>(0, place.PersonalVolume));
            Assert.Equal("marketing", repository.MarketingAddress);
            Assert.Equal((byte)2, repository.StructureNumber);
            Assert.Equal(1, repository.StructureLookupCount);
            Assert.Equal(1, repository.InviterLookupCount);
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
        var repository = new StubPlaceRepository(
            [],
            new Dictionary<string, string?>());
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IPlaceRepository>(repository);
        services.AddSingleton<IProgramUnitOfWork>(unitOfWork);
        services.AddMessageBroker(registration =>
            registration.AddConsumer<CalculateStructurePersonalVolumeRequestConsumer>());

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<CalculateStructurePersonalVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new CalculateStructurePersonalVolumeRequest(
                    "marketing",
                    structureNumber,
                    Guid.NewGuid(),
                    DateTime.UtcNow));

            Assert.NotNull(response.Message.Errors);
            Assert.Single(response.Message.Errors);
            Assert.Equal(0, repository.StructureLookupCount);
            Assert.Equal(0, repository.InviterLookupCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static Place CreatePlace(
        string? profile,
        uint placeNumber,
        bool isActive,
        uint personalVolume) =>
        Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 2,
            profileAddr: profile,
            profileLogin: profile,
            index: profile is null ? "system" : profile + placeNumber,
            placeNumber,
            parentProfileAddr: "parent",
            parentProfileLogin: "parent",
            parentPlaceNumber: 1,
            mp: $"00000000{placeNumber:X8}",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive,
            createdAt: 1,
            activatedAt: isActive ? 1 : null,
            personalVolume,
            groupVolume: 0);

    private sealed class StubPlaceRepository(
        IReadOnlyList<Place> places,
        IReadOnlyDictionary<string, string?> inviters)
        : PlaceRepositoryStub
    {
        public int StructureLookupCount { get; private set; }
        public int InviterLookupCount { get; private set; }
        public string? MarketingAddress { get; private set; }
        public byte? StructureNumber { get; private set; }

        public override Task<IReadOnlyList<Place>> GetStructurePlacesAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken)
        {
            StructureLookupCount++;
            MarketingAddress = marketingAddr;
            StructureNumber = structureNumber;
            return Task.FromResult(places);
        }

        public override Task<IReadOnlyDictionary<string, string?>> GetInvitersAsync(
            string marketingAddr,
            CancellationToken cancellationToken)
        {
            InviterLookupCount++;
            return Task.FromResult(inviters);
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
