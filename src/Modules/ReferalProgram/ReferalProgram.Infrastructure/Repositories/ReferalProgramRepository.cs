using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.ProgramAggregate;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgramAggregate = ReferalProgram.Core.ProgramAggregate.ReferalProgram;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class ReferalProgramRepository(DataContext dataContext)
    : IReferalProgramRepository
{
    public Task<ReferalProgramAggregate?> GetAsync(
        string marketingAddr,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        return dataContext.ReferalPrograms.SingleOrDefaultAsync(
            program => program.MarketingAddr == marketingAddr,
            cancellationToken);
    }
}
