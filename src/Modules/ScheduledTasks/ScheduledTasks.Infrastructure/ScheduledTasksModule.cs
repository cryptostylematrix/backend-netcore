using IntegrationRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using ScheduledTasks.Application;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Core.Scheduling;
using ScheduledTasks.Core.TaskAggregate;
using ScheduledTasks.Infrastructure.Persistence;

namespace ScheduledTasks.Infrastructure;

public static class ScheduledTasksModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScheduledTasksModule(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Tasks");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Tasks' is not configured.");
            }

            services.AddDbContext<ScheduledTasksDataContext>(options =>
                options.UseNpgsql(connectionString));
            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IScheduledTaskRepository, ScheduledTaskRepository>();
            services.AddScoped<IScheduledTasksUnitOfWork>(provider =>
                provider.GetRequiredService<ScheduledTasksDataContext>());
            services.AddScoped<IScheduledTaskQueries, ScheduledTaskQueries>();
            services.AddScoped<IIntegrationRequestDispatcher,
                MassTransitIntegrationRequestDispatcher>();
            services.AddScoped<ScheduledTaskCommandExecutor>();
            services.AddScoped<IScheduledTaskRunner, ScheduledTaskRunner>();
            services.AddScoped<IMarketingTaskBlocker, MarketingTaskBlocker>();
            services.AddSingleton<TaskCommandDocumentParser>();
            services.AddSingleton<TaskScheduleCalculator>();

            return services;
        }
    }
}
