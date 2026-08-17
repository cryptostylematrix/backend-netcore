namespace ReferalProgram.Application.Abstractions;

public interface IProgramStatisticsQueries
{
    Task<ProgramStatisticsResponse?> GetAsync(
        string marketingAddr,
        string profileAddr,
        CancellationToken cancellationToken);
}
