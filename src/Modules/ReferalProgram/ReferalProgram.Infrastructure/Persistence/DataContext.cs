using Common.Domain;
using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace ReferalProgram.Infrastructure.Persistence;

public sealed class DataContext : DbContext, IProgramUnitOfWork
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
    public DbSet<PositionLock> PositionLocks => Set<PositionLock>();
    public DbSet<MarketingTask> MarketingTasks => Set<MarketingTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? ownedTransaction = null;
        try
        {
            if (Database.CurrentTransaction is null)
                ownedTransaction = await Database.BeginTransactionAsync(cancellationToken);

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

            var result = await base.SaveChangesAsync(cancellationToken);
            if (ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            if (ownedTransaction is not null)
                await ownedTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
