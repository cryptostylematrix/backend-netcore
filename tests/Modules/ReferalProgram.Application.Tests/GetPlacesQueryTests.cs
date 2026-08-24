using Common.Dto;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class GetPlacesQueryTests
{
    [Fact]
    public async Task Passes_matrix_metrics_and_not_closed_filter_to_query()
    {
        var placeQueries = new CapturingPlaceQueries();
        var handler = new GetPlacesQueryHandler(
            placeQueries,
            new StructureQueriesStub(new StructureResponse
            {
                MarketingAddr = "marketing",
                StructureNumber = 2,
                Width = 2,
                Height = 2
            }));

        var result = await handler.Handle(
            new GetPlacesQuery(
                "marketing",
                2,
                "profile",
                3,
                25,
                OnlyNotClosed: true),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, placeQueries.MatrixSize);
        Assert.True(placeQueries.IsMatrixStructure);
        Assert.True(placeQueries.OnlyNotClosed);
        Assert.Equal(3, placeQueries.Page);
        Assert.Equal(25, placeQueries.PageSize);
    }

    [Fact]
    public async Task Missing_structure_preserves_empty_paginated_response()
    {
        var placeQueries = new CapturingPlaceQueries();
        var handler = new GetPlacesQueryHandler(
            placeQueries,
            new StructureQueriesStub(null));

        var result = await handler.Handle(
            new GetPlacesQuery("marketing", 9, "profile", 0, 20, false),
            default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.False(placeQueries.WasCalled);
    }

    [Fact]
    public async Task Zero_dimension_is_not_treated_as_a_matrix_structure()
    {
        var placeQueries = new CapturingPlaceQueries();
        var handler = new GetPlacesQueryHandler(
            placeQueries,
            new StructureQueriesStub(new StructureResponse
            {
                MarketingAddr = "marketing",
                StructureNumber = 0,
                Width = 0,
                Height = 5
            }));

        var result = await handler.Handle(
            new GetPlacesQuery("marketing", 0, "profile", 1, 20, true),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, placeQueries.MatrixSize);
        Assert.False(placeQueries.IsMatrixStructure);
        Assert.True(placeQueries.OnlyNotClosed);
    }

    private sealed class CapturingPlaceQueries : PlaceQueriesStub
    {
        public bool WasCalled { get; private set; }
        public long MatrixSize { get; private set; }
        public bool IsMatrixStructure { get; private set; }
        public bool OnlyNotClosed { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        public override Task<Paginated<PlaceWithMatrixResponse>> GetPlacesAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            long matrixSize,
            bool isMatrixStructure,
            bool onlyNotClosed,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            MatrixSize = matrixSize;
            IsMatrixStructure = isMatrixStructure;
            OnlyNotClosed = onlyNotClosed;
            Page = page;
            PageSize = pageSize;

            return Task.FromResult(new Paginated<PlaceWithMatrixResponse>
            {
                Items = Array.Empty<PlaceWithMatrixResponse>(),
                Page = page,
                TotalPages = 1
            });
        }
    }

    private sealed class StructureQueriesStub(StructureResponse? structure)
        : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(structure);
    }
}
