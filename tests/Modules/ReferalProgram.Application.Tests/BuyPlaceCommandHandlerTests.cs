using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class BuyPlaceCommandHandlerTests
{
    [Fact]
    public async Task Does_not_save_new_place_when_command_response_cannot_be_resolved()
    {
        var repository = new Repository();
        var unitOfWork = new UnitOfWork();
        var handler = new BuyPlaceCommandHandler(
            repository,
            new Structures(),
            new BuyPolicy(),
            new SourceResolver(result: null),
            unitOfWork);

        var result = await handler.Handle(Command(), default);

        Assert.False(result.IsSuccess);
        Assert.NotNull(repository.AddedPlace);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Saves_new_place_after_command_response_is_resolved()
    {
        var repository = new Repository();
        var unitOfWork = new UnitOfWork();
        var sourceAggregate = CreateSourcePlace();
        var handler = new BuyPlaceCommandHandler(
            repository,
            new Structures(),
            new BuyPolicy(),
            new SourceResolver(new SourcePlaceResolution(1, sourceAggregate)),
            unitOfWork);

        var result = await handler.Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedPlace);
        Assert.Contains(repository.AddedPlace.DomainEvents, domainEvent =>
            domainEvent is ProfileVolumeOperationDomainEvent volume
            && volume.Operation == ProfileVolumeOperation.BuyPlace);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static BuyPlaceCommand Command() => new(
        MarketingAddr: "marketing",
        StructureNumber: 2,
        ProfileAddr: "buyer-profile",
        ProfileLogin: "buyer",
        TaskKey: 10,
        QueryId: 20,
        SourceAddr: "wallet",
        Kind: BuyPlaceKind.Regular,
        ChildPosition: null);

    private sealed class Repository : PlaceRepositoryStub
    {
        private readonly Place _parent = Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 2,
            profileAddr: "parent-profile",
            profileLogin: "parent-login",
            index: "parent1",
            placeNumber: 1,
            parentProfileAddr: "root-profile",
            parentProfileLogin: "root",
            parentPlaceNumber: 1,
            mp: "00000000",
            posGroup: 0,
            kind: 0,
            pos: 1,
            filling: 0,
            deep: 1,
            isActive: true,
            createdAt: 1,
            activatedAt: 1);

        public Place? AddedPlace { get; private set; }

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) => Task.FromResult<Place?>(_parent);

        public override Task<uint> GetNextPlaceNumberAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            CancellationToken cancellationToken) => Task.FromResult<uint>(1);

        public override void Add(Place place) => AddedPlace = place;
    }

    private sealed class Structures : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<StructureResponse?>(new StructureResponse
            {
                MarketingAddr = marketingAddr,
                StructureNumber = structureNumber,
                Height = 2
            });
    }

    private sealed class BuyPolicy : IBuyPlacePolicy
    {
        public Task<BuyPlaceDecision> EvaluateAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            RequestedPosition? requestedPosition,
            CancellationToken cancellationToken) =>
            Task.FromResult(new BuyPlaceDecision(
                CanBuy: true,
                Kind: BuyPlaceKind.Regular,
                CommandTag: ProgramCommandTags.BuyPlace,
                IncludePosition: false,
                Position: new NextPosResponse
                {
                    Mp = "0000000000000001",
                    ProfileAddr = "parent-profile",
                    PlaceNumber = 1,
                    Pos = 1,
                    PosGroup = 1
                },
                Reason: null));

        public BuyPositionDecision EvaluatePosition(
            BuyPlaceDecision decision,
            PlaceResponse? parent,
            string mp,
            uint position,
            bool isLocked) => throw new NotSupportedException();
    }

    private sealed class SourceResolver(SourcePlaceResolution? result)
        : ISourcePlaceResolver
    {
        public Task<SourcePlaceResolution?> ResolveAsync(
            Place place,
            byte structureHeight,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private static Place CreateSourcePlace() => Place.Create(
        1, "marketing", 2, "parent-profile", "parent-login", "parent1", 1,
        "root-profile", "root", 1, "00000000", 0, 0, 1, 0, 1,
        true, 1, 1);

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose() { }
    }
}
