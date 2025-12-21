namespace Matrix.Application.Features.Places;

public sealed record GetPathQuery(string RootAddr, string PlaceAddr): 
    IQuery<IEnumerable<PlaceResponse>>;
    
    
internal sealed class GetPathQueryHandler(IPlaceReadOnlyRepository placeReadOnlyRepository, IMapper mapper) : 
    IQueryHandler<GetPathQuery, IEnumerable<PlaceResponse>>
{
    public async Task<Result<IEnumerable<PlaceResponse>>> Handle(GetPathQuery request, CancellationToken ct)
    {
        var rootTask = placeReadOnlyRepository.GetPlaceByAddress(request.RootAddr);
        var targetTask = placeReadOnlyRepository.GetPlaceByAddress(request.PlaceAddr);

        await Task.WhenAll(rootTask, targetTask);

        var rootPlace = rootTask.Result;
        var targetPlace = targetTask.Result;

        if (rootPlace is null || targetPlace is null)
            return Result<IEnumerable<PlaceResponse>>.NotFound();

        if (rootPlace.M != targetPlace.M)
            return Result<IEnumerable<PlaceResponse>>.Invalid();
        
        
        var rootMp = rootPlace.Mp;
        var targetMp = targetPlace.Mp;

        bool rootIsAncestor = targetMp.StartsWith(rootMp, StringComparison.Ordinal);
        bool targetIsAncestor = rootMp.StartsWith(targetMp, StringComparison.Ordinal);

        if (!rootIsAncestor && !targetIsAncestor)
            return Result<IEnumerable<PlaceResponse>>.NotFound();

        var shortMp = rootIsAncestor ? rootMp : targetMp;
        var longMp  = rootIsAncestor ? targetMp : rootMp;

        var path = new List<PlaceResponse>();
        var currentMp = longMp;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var row = await placeReadOnlyRepository.GetPlaceByMp(rootPlace.M, currentMp);
            if (row is null)
                return Result<IEnumerable<PlaceResponse>>.NotFound();

            path.Add(mapper.Map<PlaceResponse>(row));
            
            if (currentMp == shortMp)
                break;

            if (currentMp.Length == 0)
                return Result<IEnumerable<PlaceResponse>>.NotFound();

            currentMp = currentMp.Substring(0, currentMp.Length - 1);

            if (currentMp.Length < shortMp.Length)
                return Result<IEnumerable<PlaceResponse>>.NotFound();
        }

        path.Reverse();
        return Result<IEnumerable<PlaceResponse>>.Success(path);
    }

}