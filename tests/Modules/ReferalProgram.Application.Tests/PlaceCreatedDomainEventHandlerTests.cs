using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Registers_child_and_increments_matrix_ancestors()
    {
        var repository = new Repository();
        var handler = new PlaceCreatedDomainEventHandler(repository);

        await handler.Handle(
            new PlaceCreatedDomainEvent(repository.Parent.Id, expectedParentFilling: 0),
            default);

        Assert.Equal<uint>(1, repository.Parent.Filling);
        Assert.Equal(1, repository.Parent.MatrixFilling);
        Assert.Equal(repository.Parent.Id, repository.IncrementParentId);
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public Place Parent { get; } = Place.Create(
            parentId: 10,
            marketingAddr: "marketing",
            structureNumber: 2,
            profileAddr: "parent-profile",
            profileLogin: "parent",
            index: "parent1",
            placeNumber: 1,
            parentProfileAddr: "root-profile",
            parentProfileLogin: "root",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: 0,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: true,
            createdAt: 1,
            activatedAt: 1,
            personalVolume: 0,
            groupVolume: 0,
            taskKey: 0,
            taskQueryId: 0,
            taskSourceAddr: null);

        public int? IncrementParentId { get; private set; }

        public override Task<Place?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult<Place?>(Parent);

        public override Task IncrementMatrixFillingForAncestorsAsync(
            int parentId,
            CancellationToken cancellationToken)
        {
            IncrementParentId = parentId;
            return Task.CompletedTask;
        }
    }
}
