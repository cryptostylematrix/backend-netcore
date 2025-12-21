using FastEndpoints;
using Matrix.Application;
using Matrix.Domain.Services;
using Matrix.Infrastructure.Data;
using Matrix.Infrastructure.Data.Repositories;
using Matrix.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Matrix.Infrastructure;

public static class MatrixModule
{
    public static IServiceCollection AddMatrixModule(this IServiceCollection services,
        IConfiguration configuration)
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

        // services
        services.AddScoped<IFindNextPosService, FindNextPosService>();

        return services;
    }
    
    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Matrix")!;

        // EF Core
        services.AddDbContext<MatrixDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dapper mapping
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Dapper connection factory (ONE place)
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        // Repositories
        services.AddScoped<ILockRepository, LockRepository>();
        services.AddScoped<ILockReadOnlyRepository, LockReadOnlyRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IPlaceReadOnlyRepository, PlaceReadOnlyRepository>();

        services.AddScoped<IMatrixUnitOfWork>(sp => sp.GetRequiredService<MatrixDbContext>());

        services.AddHealthChecks().AddDbContextCheck<MatrixDbContext>();
        
        // Mapper
        services.AddAutoMapper(cfg => { }, ApplicationReference.Assembly);
    }
}