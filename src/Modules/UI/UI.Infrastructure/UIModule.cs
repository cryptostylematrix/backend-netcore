using Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UI.Application;
using UI.Application.Abstractions;
using UI.Core.ProfileAggregate;
using UI.Core.WalletProfileIntentAggregate;
using UI.Infrastructure.Persistence;
using UI.Infrastructure.Queries;
using UI.Infrastructure.Repositories;
using UI.Infrastructure.Services;

namespace UI.Infrastructure;

public static class UIModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddUIModule(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("UI");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = configuration.GetConnectionString("Programs");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'UI' or fallback 'Programs' is not configured.");
            }

            services.AddUIApplication();
            services.AddKeyedSingleton<NpgsqlDataSource>("UI", (_, _) =>
                NpgsqlDataSource.Create(connectionString));
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString));
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IUiUnitOfWork>(provider =>
                provider.GetRequiredService<DataContext>());
            services.AddScoped<ICachedProfileRepository, CachedProfileRepository>();
            services.AddScoped<IWalletProfileIntentRepository,
                WalletProfileIntentRepository>();
            services.AddScoped<IWalletProfileIntentEventRepository,
                WalletProfileIntentEventRepository>();
            services.AddScoped<IWalletProfileQueries, WalletProfileQueries>();
            services.AddSingleton<IWalletAddressService, WalletAddressService>();
            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
