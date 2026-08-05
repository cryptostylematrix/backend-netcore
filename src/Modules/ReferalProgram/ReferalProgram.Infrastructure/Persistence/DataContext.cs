using Common.Domain;
using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Infrastructure.Persistence;

public sealed class DataContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public DataContext(
        DbContextOptions<DataContext> options,
        IDomainEventDispatcher? domainEventDispatcher = null)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Place> Places => Set<Place>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_domainEventDispatcher is not null)
        {
            while (true)
            {
                var entitiesWithEvents = ChangeTracker
                    .Entries()
                    .Select(entry => entry.Entity)
                    .OfType<IEntity>()
                    .Where(entity => entity.DomainEvents.Count > 0)
                    .ToArray();

                if (entitiesWithEvents.Length == 0)
                    break;

                await _domainEventDispatcher.DispatchAndClearEventsAsync(
                    entitiesWithEvents,
                    cancellationToken);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
