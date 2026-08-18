using Common.Domain;
using Microsoft.EntityFrameworkCore;
using UI.Core.ProfileAggregate;
using UI.Core.WalletProfileIntentAggregate;
using UI.Application.Abstractions;

namespace UI.Infrastructure.Persistence;

public sealed class DataContext : DbContext, IUiUnitOfWork
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public DataContext(
        DbContextOptions<DataContext> options,
        IDomainEventDispatcher? domainEventDispatcher = null)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<CachedProfile> Profiles => Set<CachedProfile>();
    public DbSet<WalletProfileIntent> WalletProfileIntents => Set<WalletProfileIntent>();
    public DbSet<WalletProfileIntentEvent> WalletProfileIntentEvents =>
        Set<WalletProfileIntentEvent>();

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
}
