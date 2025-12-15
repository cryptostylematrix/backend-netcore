namespace Matrix.Infrastructure.Data.Repositories;

public sealed class MultiPlaceRepository : IMultiPlaceRepository
{
    public Task<PlacesResult> GetPlaces(int m, string profileAddr, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPlacesCount(int m, string profileAddr)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace?> GetRootPlace(int m, string profileAddr)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace?> GetPlaceByTaskKey(int taskKey)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetMaxPlaceNumber(int m, string profileAddr)
    {
        throw new NotImplementedException();
    }

    public Task IncrementFilling(int id)
    {
        throw new NotImplementedException();
    }

    public Task IncrementFilling2(int id)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace> UpdatePlaceAddressAndConfirm(int id, string addr)
    {
        throw new NotImplementedException();
    }

    public Task<PlacesResult> SearchPlaces(int m, string profileAddr, string query, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace?> GetPlaceByAddress(string addr)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPlacesCountByMpPrefix(int m, string mpPrefix)
    {
        throw new NotImplementedException();
    }

    public Task<PlacesResult> GetPlacesByMpPrefix(int m, string mpPrefix, int depthLevels, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace?> GetPlaceByMp(int m, string mp)
    {
        throw new NotImplementedException();
    }

    public Task<PlacesResult> GetOpenPlacesByMpPrefix(int m, string mpPrefix, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace> AddPlace(MultiPlace multiPlace)
    {
        throw new NotImplementedException();
    }
}