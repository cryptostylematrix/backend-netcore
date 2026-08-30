using Microsoft.EntityFrameworkCore;
using ScheduledTasks.Core.TaskAggregate;
using ScheduledTasks.Application.Abstractions;

namespace ScheduledTasks.Infrastructure.Persistence;

public sealed class ScheduledTasksDataContext(
    DbContextOptions<ScheduledTasksDataContext> options)
    : DbContext(options), IScheduledTasksUnitOfWork
{
    public DbSet<ScheduledTask> Tasks => Set<ScheduledTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScheduledTasksDataContext).Assembly);
    }

    public async Task<bool> TrySaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
            ChangeTracker.Clear();
            return false;
        }
    }
}
