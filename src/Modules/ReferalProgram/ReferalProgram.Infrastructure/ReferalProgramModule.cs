using Common.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReferalProgram.Application;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Policies;
using ReferalProgram.Application.Services.PositionStrategies;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;
using ReferalProgram.Infrastructure.Repositories;
using ReferalProgram.Infrastructure.Services;

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
            services.AddScoped<IProgramUnitOfWork>(provider =>
                provider.GetRequiredService<DataContext>());
            services.AddScoped<IPlaceRepository, PlaceRepository>();
            services.AddScoped<IPositionLockRepository, PositionLockRepository>();
            services.AddScoped<IMarketingTaskRepository, MarketingTaskRepository>();

            services.AddScoped<IPlaceQueries, PlaceQueries>();
            services.AddScoped<ILockQueries, LockQueries>();
            services.AddScoped<IPositionCandidateQueries>(provider =>
                (IPositionCandidateQueries)provider.GetRequiredService<IPlaceQueries>());
            services.AddScoped<IPositionLockQueries>(provider =>
                (IPositionLockQueries)provider.GetRequiredService<ILockQueries>());
            services.AddScoped<IStructureQueries, StructureQueries>();
            services.AddScoped<IStructureRankQueries, StructureRankQueries>();
            services.AddScoped<IProgramStatisticsQueries, ProgramStatisticsQueries>();
            services.AddScoped<IProfileRootPlaceResolver, ProfileRootPlaceResolver>();
            services.AddScoped<IPositionRootResolver, PositionRootResolver>();
            services.AddScoped<INextPositionQueries, NextPositionQueries>();
            services.AddSingleton<IPositionAlgorithmConfigurationParser,
                PositionAlgorithmConfigurationParser>();
            services.AddSingleton<IPositionGroupSelector, PositionGroupSelector>();
            services.AddScoped<IPositionAlgorithmResolver, PositionAlgorithmResolver>();
            services.AddScoped<IProgramCommandQueries, ProgramCommandQueries>();
            services.AddScoped<IBuyPlacePolicy, BuyPlacePolicy>();
            services.AddScoped<IClonePlaceKindPolicy, ClonePlaceKindPolicy>();
            services.AddScoped<ISourcePlaceResolver, SourcePlaceResolver>();
            services.AddScoped<IRelativePlaceResolver, RelativePlaceResolver>();
            services.AddSingleton<ITonAddressComparer, TonAddressComparer>();
            services.AddSingleton<IPositionLockPolicy, PositionLockPolicy>();
            services.AddScoped<IPositionNodeActionPolicy, PositionNodeActionPolicy>();
            services.AddScoped<IRootPlaceStrategy, OwnerRootPlaceStrategy>();
            services.AddScoped<IRootPlaceStrategy, ProfileRootPlaceStrategy>();
            services.AddScoped<IPositionAlgorithmStrategy, ChessPositionAlgorithmStrategy>();
            services.AddScoped<IPositionAlgorithmStrategy, RadarPositionAlgorithmStrategy>();
            services.AddScoped<IPositionAlgorithmStrategy, ClassicPositionAlgorithmStrategy>();
            services.AddScoped<IPositionAlgorithmStrategy,
                TrimmedClassicPositionAlgorithmStrategy>();
            services.AddScoped<INextPosService, NextPosService>();
            services.AddScoped<IReferalProgramQueries, ReferalProgramQueries>();

            return services;
        }
    }
}
