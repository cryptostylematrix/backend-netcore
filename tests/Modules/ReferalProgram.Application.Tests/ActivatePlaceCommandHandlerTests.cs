using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ActivatePlaceCommandHandlerTests
{
    [Fact]
    public async Task Activates_place_and_returns_resolved_source()
    {
        var repository = new Repository(includeCuratorPlace: true);
        var unitOfWork = new UnitOfWork();
        var handler = Handler(repository, unitOfWork, setActive: true);

        var result = await handler.Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.True(repository.Target.IsActive);
        Assert.NotNull(repository.Target.ActivatedAt);
        Assert.Equal<uint>(0, repository.CuratorPlace!.PersonalVolume);
        Assert.Equal<uint>(7, result.Value.Code);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Can_confirm_activity_without_changing_inactive_status_or_curator_place()
    {
        var repository = new Repository(includeCuratorPlace: false);
        var unitOfWork = new UnitOfWork();
        var handler = Handler(repository, unitOfWork, setActive: false);

        var result = await handler.Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.False(repository.Target.IsActive);
        Assert.NotNull(repository.Target.ActivatedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static ActivatePlaceCommandHandler Handler(
        Repository repository,
        UnitOfWork unitOfWork,
        bool setActive) => new(
            repository,
            new Policy(setActive),
            new Structures(),
            new SourceResolver(),
            unitOfWork);

    private static ActivatePlaceCommand Command() => new(
        MarketingAddr: "marketing",
        StructureNumber: 2,
        ProfileAddr: "referral",
        PlaceNumber: 1,
        TaskKey: 10,
        QueryId: 20,
        SourceAddr: "payer");

    private sealed class Policy(bool setActive) : IActivatePlacePolicy
    {
        public ActivatePlaceDecision Evaluate(StructureResponse structure, IReadOnlySet<uint> availableCommandTags, PlaceResponse? place) => throw new NotSupportedException();

        public Task<ActivatePlaceDecision> EvaluateAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ActivatePlaceDecision(
                true,
                ProgramCommandTags.ActivatePlace,
                setActive,
                null));
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
                Height = 1
            });
    }

    private sealed class SourceResolver : ISourcePlaceResolver
    {
        public Task<SourcePlaceResolution?> ResolveAsync(
            Place place,
            byte structureHeight,
            CancellationToken cancellationToken) =>
            Task.FromResult<SourcePlaceResolution?>(new(7, place));
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public Repository(bool includeCuratorPlace)
        {
            Target = CreatePlace(2, "referral", "referral", "parent", false, null);
            Invite = CreatePlace(0, "referral", "referral", "curator", false, null);
            CuratorPlace = includeCuratorPlace
                ? CreatePlace(2, "curator", "curator", "root", true, 1)
                : null;
        }

        public Place Target { get; }
        public Place Invite { get; }
        public Place? CuratorPlace { get; }

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken)
        {
            Place? place = (structureNumber, profileAddr, placeNumber) switch
            {
                (2, "referral", 1) => Target,
                (0, "referral", 1) => Invite,
                (2, "curator", 1) => CuratorPlace,
                _ => null
            };
            return Task.FromResult(place);
        }
    }

    private static Place CreatePlace(
        byte structureNumber,
        string profileAddr,
        string profileLogin,
        string parentProfileAddr,
        bool isActive,
        long? activatedAt) => Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber,
            profileAddr,
            profileLogin,
            index: profileLogin + "1",
            placeNumber: 1,
            parentProfileAddr,
            parentProfileLogin: parentProfileAddr,
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive,
            createdAt: 1,
            activatedAt,
            personalVolume: 0,
            groupVolume: 0);

    private sealed class UnitOfWork : IProgramUnitOfWork
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
