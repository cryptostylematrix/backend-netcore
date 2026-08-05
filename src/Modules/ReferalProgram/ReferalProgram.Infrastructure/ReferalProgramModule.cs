using Common.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReferalProgram.Application;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;
using ReferalProgram.Infrastructure.Repositories;

namespace ReferalProgram.Infrastructure;

public static class ReferalProgramModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddReferalProgramModule(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Programs");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Programs' is not configured.");
            }

            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(ApplicationReference.Assembly);
            });

            services.AddKeyedSingleton<NpgsqlDataSource>("Programs", (_, _) =>
            {
                return NpgsqlDataSource.Create(connectionString);
            });

            services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DataContext>());
            services.AddScoped<IPlaceRepository, PlaceRepository>();

            services.AddScoped<IPlaceQueries, PlaceQueries>();
            services.AddScoped<ILockQueries, LockQueries>();
            services.AddScoped<IStructureQueries, StructureQueries>();
            services.AddScoped<INextPosService, NextPosService>();
            services.AddScoped<IMarketingTaskStore, MarketingTaskStore>();
            services.AddScoped<IReferalProgramQueries, ReferalProgramQueries>();

            return services;
        }
    }
}
