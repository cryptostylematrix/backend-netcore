namespace ReferalProgram.Application.Abstractions;

public interface IReferalProgramQueries
{
    Task<IReadOnlyCollection<ReferalProgramResponse>> GetAllAsync(
        CancellationToken cancellationToken);
}
