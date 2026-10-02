using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Structures;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramStructuresQueryTests
{
    [Fact]
    public async Task Uses_registered_address_and_includes_structure_zero()
    {
        var source = new Structures();
        var handler = new GetProgramStructuresQueryHandler(source, new Programs(), new Addresses());
        var result = await handler.Handle(new("equivalent-address"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("registered-address", source.Address);
        Assert.Equal(new byte[] { 0, 2 }, result.Value.Select(row => row.StructureNumber));
    }

    [Fact]
    public async Task Unknown_program_does_not_read_structures()
    {
        var source = new Structures();
        var handler = new GetProgramStructuresQueryHandler(source, new Programs(), new Addresses());
        var result = await handler.Handle(new("unknown"), default);
        Assert.Equal(Ardalis.Result.ResultStatus.NotFound, result.Status);
        Assert.Null(source.Address);
    }

    [Fact]
    public async Task Structure_failure_is_not_reported_as_an_empty_list()
    {
        var handler = new GetProgramStructuresQueryHandler(new Structures { Fail = true }, new Programs(), new Addresses());
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new("registered-address"), default));
    }

    private sealed class Structures : IProgramStructureListQueries
    {
        public string? Address { get; private set; }
        public bool Fail { get; init; }
        public Task<IReadOnlyList<StructureResponse>> GetAsync(string marketingAddress, CancellationToken ct)
        {
            if (Fail) throw new InvalidOperationException("Unavailable");
            Address = marketingAddress;
            return Task.FromResult<IReadOnlyList<StructureResponse>>([
                new() { MarketingAddr = marketingAddress, StructureNumber = 0 },
                new() { MarketingAddr = marketingAddress, StructureNumber = 2 }
            ]);
        }
    }

    private sealed class Programs : IReferalProgramQueries
    {
        public Task<IReadOnlyCollection<ReferalProgramResponse>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<ReferalProgramResponse>>([new() { MarketingAddr = "registered-address" }]);
    }

    private sealed class Addresses : ITonAddressComparer
    {
        public bool AreEqual(string? left, string? right) =>
            left == "registered-address" && right is "registered-address" or "equivalent-address";
    }
}
