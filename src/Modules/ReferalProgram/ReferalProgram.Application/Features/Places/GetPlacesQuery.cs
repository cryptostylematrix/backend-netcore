using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Features.Places;

public sealed record GetPlacesQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    int Page,
    int PageSize,
    bool OnlyNotClosed) : IQuery<Paginated<PlaceWithMatrixResponse>>;

internal sealed class GetPlacesQueryHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries)
    : IQueryHandler<GetPlacesQuery, Paginated<PlaceWithMatrixResponse>>
{
    public async Task<Result<Paginated<PlaceWithMatrixResponse>>> Handle(
        GetPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);

        if (structure is null)
        {
            return Result.Success(new Paginated<PlaceWithMatrixResponse>
            {
                Items = Array.Empty<PlaceWithMatrixResponse>(),
                Page = request.Page > 0 ? request.Page : 1,
                TotalPages = 1
            });
        }

        long matrixSize;
        try
        {
            matrixSize = MatrixSizeCalculator.Calculate(
                structure.Width,
                structure.Height);
        }
        catch (OverflowException)
        {
            return Result<Paginated<PlaceWithMatrixResponse>>.Error(
                "The configured matrix size exceeds the supported range.");
        }

        var places = await placeQueries.GetPlacesAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            matrixSize,
            structure.Width > 0 && structure.Height > 0,
            request.OnlyNotClosed,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(places);
    }
}
