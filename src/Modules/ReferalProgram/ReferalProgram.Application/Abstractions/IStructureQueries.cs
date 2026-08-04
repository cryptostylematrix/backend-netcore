namespace ReferalProgram.Application.Abstractions;

public interface IStructureQueries
{
    Task<StructureResponse?> GetStructureAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);
}
