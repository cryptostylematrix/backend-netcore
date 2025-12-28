using FastEndpoints;
using Matrix.Application;
using Matrix.Application.Services;
using Matrix.Infrastructure.Queries;
using Matrix.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Infrastructure;

public static class MatrixModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMatrixModule(IConfiguration configuration)
        {
            services.AddFastEndpoints(options =>
            {
                options.Assemblies = [PresentationReference.Assembly];
            });
        
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(ApplicationReference.Assembly);
            });
        
            services.AddInfrastructure(configuration);
        
            
            // Services
            services.AddScoped<INextPosService, NextPosService>();

            return services;
        }

        private void AddInfrastructure(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Matrix")!;
            
            // Dapper mapping
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            // Dapper connection factory (ONE place)
            services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

            // Repositories
            services.AddScoped<IPlaceQueries, PlaceQueries>();
            services.AddScoped<ILockQueries, LockQueries>();
        }
    }
}