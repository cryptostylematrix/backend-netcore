using Contracts.Application;
using Contracts.Infrastructure.Queries;
using Contracts.Infrastructure.Ton;
using Contracts.Presentation;
using FastEndpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Contracts.Infrastructure;

public static class ContractsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddContractsModule(IConfiguration configuration)
        {
            services.AddFastEndpoints(options =>
            {
                options.Assemblies = [PresentationReference.Assembly];
            });

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(ApplicationReference.Assembly);
            });

            // Options
            services.AddOptions<TonContractAddressesOptions>()
                .Bind(configuration.GetSection("TonContractAddresses"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.MultiAddr), "TonContractAddresses:MultiAddr is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ProfileCollectionAddr), "TonContractAddresses:ProfileCollectionAddr is required")
                .ValidateOnStart();

            
            services.AddOptions<TonCenterOptions>()
                .Bind(configuration.GetSection("TonCenter"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "TonCenter.Endpoint is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "TonCenter.ApiKey is required")
                .ValidateOnStart();
            
            services.Configure<TonQueryCacheOptions>(
                configuration.GetSection("TonQueryCache"));

            services.AddInfrastructure();

            // Query services
            services.AddScoped<IInviteQueries, InviteQueries>();
            services.AddScoped<IProfileItemQueries, ProfileItemQueries>();
            services.AddScoped<IMultiQueries, MultiQueries>();
            services.AddScoped<IProfileCollectionQueries, ProfileCollectionQueries>();
            services.AddScoped<IPlaceQueries, PlaceQueries>();
            services.AddScoped<IGeneralQueries, GeneralQueries>();

            return services;
        }
        

        private void AddInfrastructure()
        {
            // Create a single shared pipeline (singleton)
            services.AddSingleton<ResiliencePipeline>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<TonCenterOptions>>().Value;

                return TonCenterPipelineFactory.Create(
                    rps: opts.RequestsPerSecond,
                    queueLimit: opts.QueueLimit,
                    acquireTimeoutMs: opts.AcquireTimeoutMs);
            });

            // Register ITonClient as: TonClient wrapped by PollyTonClient
            services.AddSingleton<ITonClient>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<TonCenterOptions>>().Value;
                var pipeline = sp.GetRequiredService<ResiliencePipeline>();

                ITonClient inner = new TonClient(
                    TonClientType.HTTP_TONCENTERAPIV2,
                    new HttpParameters
                    {
                        Endpoint = opts.Endpoint,
                        ApiKey = opts.ApiKey
                    });

                return new PollyTonClient(inner, pipeline);
            });
        }

    }
}