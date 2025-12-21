namespace Matrix.Domain.Abstractions;

public interface IPlaceRepository: IRepository<MultiPlace>
{
    Task IncrementFilling(int id);
    Task IncrementFilling2(int id);
    Task<MultiPlace> UpdatePlaceAddressAndConfirm(int id, string addr);
    Task<MultiPlace> AddPlace(MultiPlace multiPlace);
}