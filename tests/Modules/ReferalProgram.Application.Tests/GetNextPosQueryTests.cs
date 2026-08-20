using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class GetNextPosQueryTests
{
    [Fact]
    public async Task Omitted_operation_uses_default_configuration()
    {
        var service = new NextPosServiceStub();
        var handler = new GetNextPosQueryHandler(service);

        var result = await handler.Handle(
            new GetNextPosQuery("marketing", 2, "profile", null),
            default);

        Assert.True(result.IsSuccess);
        Assert.Null(service.Operation);
    }

    [Fact]
    public async Task Valid_operation_is_forwarded_to_position_service()
    {
        var service = new NextPosServiceStub();
        var handler = new GetNextPosQueryHandler(service);

        var result = await handler.Handle(
            new GetNextPosQuery(
                "marketing",
                2,
                "profile",
                PositionOperationNames.CreateReinvest),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PositionOperation.CreateReinvest, service.Operation);
    }

    [Fact]
    public async Task Unknown_operation_is_rejected()
    {
        var service = new NextPosServiceStub();
        var handler = new GetNextPosQueryHandler(service);

        var result = await handler.Handle(
            new GetNextPosQuery("marketing", 2, "profile", "unknown"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Null(service.Operation);
        Assert.Contains("Unknown position operation", Assert.Single(result.Errors));
    }

    private sealed class NextPosServiceStub : INextPosService
    {
        public PositionOperation? Operation { get; private set; }

        public Task<PositionSelection?> ResolveSelectionAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct) => throw new NotSupportedException();

        public Task<NextPosResponse?> FindNextAsync(
            PositionSelection selection,
            CancellationToken ct) => throw new NotSupportedException();

        public Task<NextPosResponse?> GetNextPosAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct)
        {
            Operation = operation;
            return Task.FromResult<NextPosResponse?>(new NextPosResponse
            {
                ProfileAddr = "parent",
                PlaceNumber = 1,
                Pos = 1,
                Mp = "00000001"
            });
        }
    }
}
