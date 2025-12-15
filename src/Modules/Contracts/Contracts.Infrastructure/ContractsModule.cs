using Contracts.Application;
using Contracts.Domain.Aggregates.Multi;
using Contracts.Domain.Aggregates.ProfileCollection;
using Contracts.Presentation;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TonSdk.Client;
using TonSdk.Core;

namespace Contracts.Infrastructure;

public static class ContractsModule
{
    public static void MapEndpoints(IApplicationBuilder app)
    {
        app.UseFastEndpoints();
    }

    public static IServiceCollection AddContractsModule(this IServiceCollection services,
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

        return services;
    }
    
    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITonClient, TonClient>(sp => new TonClient(TonClientType.HTTP_TONCENTERAPIV2, new HttpParameters
        {
            Endpoint = "https://toncenter.com/api/v2/jsonRPC",
            ApiKey = "193210c5feca89e2e483c94b7e7e43797c5c3e33cd61c7e711d4868dd8a4ed04"
        }));

        services.AddSingleton<MultiContract>(sp =>
            MultiContract.CreateFromAddress(new Address("EQCio9soCgFJxQOPMpkerdlDTWjzD_el3omcOiq9NSURzMnV")));
        
        services.AddSingleton<ProfileCollectionContract>(sp =>
            ProfileCollectionContract.CreateFromAddress(new Address("EQAiWZqRPp39Z46Y4Pahvj5UQSzafJrUiTbYDcQ0kldLebjn")));
        
        // Mapper
        services.AddAutoMapper(cfg => { }, ApplicationReference.Assembly);
    }
}