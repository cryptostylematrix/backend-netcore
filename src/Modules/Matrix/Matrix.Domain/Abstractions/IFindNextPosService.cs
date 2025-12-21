namespace Matrix.Domain.Abstractions;

public interface IFindNextPosService
{
    Task<Result<MultiPlace>> Find(short m, string profileAddr, CancellationToken cancellationToken = default);
}