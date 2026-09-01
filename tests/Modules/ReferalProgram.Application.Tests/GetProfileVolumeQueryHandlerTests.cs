using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.ProfileVolumes;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class GetProfileVolumeQueryHandlerTests
{
    [Fact]
    public async Task Returns_zero_volume_response_from_query_boundary()
    {
        var handler = new GetProfileVolumeQueryHandler(new Queries());

        var result = await handler.Handle(
            new GetProfileVolumeQuery("marketing", 2, "profile"),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("profile", result.Value.ProfileAddr);
        Assert.Equal<uint>(0, result.Value.PersonalVolume);
        Assert.Equal<uint>(0, result.Value.ReferralVolume);
        Assert.Equal<uint>(0, result.Value.GroupVolume);
    }

    private sealed class Queries : IProfileVolumeQueries
    {
        public Task<ProfileVolumeResponse> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProfileVolumeResponse
            {
                MarketingAddr = marketingAddr,
                StructureNumber = structureNumber,
                ProfileAddr = profileAddr
            });

        public Task<IReadOnlyDictionary<string, uint>> GetReferralVolumesAsync(
            string marketingAddr,
            byte structureNumber,
            IReadOnlyCollection<string> profileAddresses,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
