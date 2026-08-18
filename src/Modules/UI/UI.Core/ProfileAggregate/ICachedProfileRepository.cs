using Common.Domain;

namespace UI.Core.ProfileAggregate;

public interface ICachedProfileRepository : IRepository<CachedProfile>
{
    Task<CachedProfile?> GetByAddressAsync(
        string address,
        CancellationToken cancellationToken);

    void Add(CachedProfile profile);
}
