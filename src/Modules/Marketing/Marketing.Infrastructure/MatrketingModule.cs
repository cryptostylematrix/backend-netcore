using Marketing.Application;
using Marketing.Application.Services;
using Marketing.Infrastructure.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class MatrketingModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMarketingModule(IConfiguration configuration)
        {
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
