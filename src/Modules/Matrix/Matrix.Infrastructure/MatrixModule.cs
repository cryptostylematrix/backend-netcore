using System.Data;
using Matrix.Application;
using Matrix.Infrastructure.Data;
using Matrix.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Matrix.Infrastructure;

public static class MatrixModule
{
    public static IServiceCollection AddMatrixModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(ApplicationReference.Assembly);
        });
        
        services.AddInfrastructure(configuration);

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Matrix")!;
        
        // DbContext
        services.AddDbContext<MatrixDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        // DbConnection
        services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));
        
        // Repositories
        services.AddScoped<IMultiLockRepository, MultiLockRepository>();
        services.AddScoped<IMultiPlaceRepository, MultiPlaceRepository>();
        
        services.AddScoped<IMatrixUnitOfWork>(sp => sp.GetRequiredService<MatrixDbContext>());

       
        // Dapper

        // Health Checks
        services.AddHealthChecks().AddDbContextCheck<MatrixDbContext>();
    }
}