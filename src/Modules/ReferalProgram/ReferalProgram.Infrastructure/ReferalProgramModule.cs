using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Infrastructure;

public static class ReferalProgramModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddReferalProgramModule(IConfiguration configuration)
        {
            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(ApplicationReference.Assembly);
            });

            services.AddKeyedSingleton<NpgsqlDataSource>("Programs", (_, _) =>
            {
                var connectionString = configuration.GetConnectionString("Programs");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Connection string 'Programs' is not configured.");
                }

                return NpgsqlDataSource.Create(connectionString);
            });

            services.AddScoped<IPlaceQueries, PlaceQueries>();
            services.AddScoped<ILockQueries, LockQueries>();
            services.AddScoped<IStructureQueries, StructureQueries>();
            services.AddScoped<INextPosService, NextPosService>();
            services.AddScoped<IPlaceCommands, PlaceCommands>();
            services.AddScoped<IMarketingTaskStore, MarketingTaskStore>();
            services.AddScoped<IReferalProgramQueries, ReferalProgramQueries>();

            return services;
        }
    }
}
