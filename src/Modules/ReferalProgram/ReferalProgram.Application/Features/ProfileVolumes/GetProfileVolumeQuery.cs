namespace ReferalProgram.Application.Features.ProfileVolumes;

public sealed record GetProfileVolumeQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr) : IQuery<ProfileVolumeResponse>;

internal sealed class GetProfileVolumeQueryHandler(IProfileVolumeQueries queries)
    : IQueryHandler<GetProfileVolumeQuery, ProfileVolumeResponse>
{
    public async Task<Result<ProfileVolumeResponse>> Handle(
        GetProfileVolumeQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProfileAddr))
            return Result<ProfileVolumeResponse>.Error("ProfileAddr is required.");

        return Result.Success(await queries.GetAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken));
    }
}
