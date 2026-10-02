namespace ReferalProgram.Application.Abstractions;

public interface IProgramStructureListQueries
{
    Task<IReadOnlyList<StructureResponse>> GetAsync(string marketingAddress, CancellationToken ct);
}
