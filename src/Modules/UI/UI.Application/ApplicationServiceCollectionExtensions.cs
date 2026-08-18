using Microsoft.Extensions.DependencyInjection;
using UI.Application.Abstractions;
using UI.Application.Services;

namespace UI.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUIApplication(this IServiceCollection services)
    {
        services.AddMediatR(options =>
            options.RegisterServicesFromAssembly(ApplicationReference.Assembly));
        services.AddScoped<IProfileContractReader, ProfileContractReader>();
        services.AddScoped<ProfileSynchronizer>();
        return services;
    }
}
