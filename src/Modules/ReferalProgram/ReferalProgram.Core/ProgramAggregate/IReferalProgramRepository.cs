using Common.Domain;

namespace ReferalProgram.Core.ProgramAggregate;

public interface IReferalProgramRepository : IRepository<ReferalProgram>
{
    Task<ReferalProgram?> GetAsync(
        string marketingAddr,
        CancellationToken cancellationToken);
}
