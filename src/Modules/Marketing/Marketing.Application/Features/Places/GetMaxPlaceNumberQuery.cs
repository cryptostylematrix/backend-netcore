namespace Marketing.Application.Features.Places;

public sealed record GetMaxPlaceNumberQuery(
    string MarketingAddr,
    byte M,
    string ProfileAddr) : IQuery<uint>;

internal sealed class GetMaxPlaceNumberQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetMaxPlaceNumberQuery, uint>
{
    public async Task<Result<uint>> Handle(
        GetMaxPlaceNumberQuery request,
        CancellationToken cancellationToken)
    {
        var maxPlaceNumber = await placeQueries.GetMaxPlaceNumberAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            cancellationToken);

        return Result.Success(maxPlaceNumber);
    }
}