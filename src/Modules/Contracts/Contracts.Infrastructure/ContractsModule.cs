using Contracts.Application;
using Contracts.Infrastructure.Queries;
using Contracts.Infrastructure.Ton;
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

            services.AddOptions<ProcessorWalletOptions>()
                .Bind(configuration.GetSection(ProcessorWalletOptions.SectionName))
                .Validate(
                    options => options.Mnemonic.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries).Length == 24,
                    "ProcessorWallet:Mnemonic must contain 24 words.")
                .Validate(options =>
                        decimal.TryParse(
                            options.TransferAmountTon,
                            System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var amount) && amount > 0,
                    "ProcessorWallet:TransferAmountTon must be greater than zero.")
                .Validate(options => options.SeqnoTimeoutSeconds > 0,
                    "ProcessorWallet:SeqnoTimeoutSeconds must be greater than zero.")
                .Validate(options => options.PollIntervalMilliseconds > 0,
                    "ProcessorWallet:PollIntervalMilliseconds must be greater than zero.")
                .Validate(options => options.MaxRetries >= 0,
                    "ProcessorWallet:MaxRetries cannot be negative.")
                .Validate(options => options.RetryDelayMilliseconds > 0,
                    "ProcessorWallet:RetryDelayMilliseconds must be greater than zero.")
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
            services.AddScoped<IWalletQueries, WalletQueries>();
            services.AddScoped<IMarketingQueries, MarketingQueries>();
            services.AddScoped<IMarketingV3Queries, MarketingV3Queries>();
            services.AddSingleton<IMarketingTransactionSender, MarketingTransactionSender>();
            services.AddScoped<IMatrixPlaceQueries, MatrixPlaceQueries>();
            services.AddScoped<IJetttonMinterQueries, JetttonMinterQueries>();
            services.AddScoped<IJettonWalletQueries, JettonWalletQueries>();

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
