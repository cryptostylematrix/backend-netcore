using Microsoft.EntityFrameworkCore;
using UI.Core.ProfileAggregate;
using UI.Infrastructure.Persistence;

namespace UI.Infrastructure.Repositories;

internal sealed class CachedProfileRepository(DataContext dataContext)
    : ICachedProfileRepository
{
    public Task<CachedProfile?> GetByAddressAsync(
        string address,
        CancellationToken cancellationToken) =>
        dataContext.Profiles.SingleOrDefaultAsync(
            profile => profile.Address == address,
            cancellationToken);

    public void Add(CachedProfile profile) => dataContext.Profiles.Add(profile);
}
