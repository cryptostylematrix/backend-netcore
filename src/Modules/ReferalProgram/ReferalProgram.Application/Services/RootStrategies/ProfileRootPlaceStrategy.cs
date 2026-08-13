namespace ReferalProgram.Application.Services.RootStrategies;

public sealed class ProfileRootPlaceStrategy(
    IProfileRootPlaceResolver profileRootPlaceResolver) : IRootPlaceStrategy
{
    public string Name => "profile";

    public Task<PlaceResponse?> ResolveAsync(
        RootPlaceStrategyContext context,
        CancellationToken cancellationToken) =>
        profileRootPlaceResolver.ResolveAsync(
            context.MarketingAddr,
            context.StructureNumber,
            context.ProfileAddr,
            cancellationToken);
}
