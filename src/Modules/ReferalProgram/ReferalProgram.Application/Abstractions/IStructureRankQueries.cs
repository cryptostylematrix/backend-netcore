namespace ReferalProgram.Application.Abstractions;

public interface IStructureRankQueries
{
    Task<IReadOnlyCollection<StructureRankResponse>> GetAllAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);
}
