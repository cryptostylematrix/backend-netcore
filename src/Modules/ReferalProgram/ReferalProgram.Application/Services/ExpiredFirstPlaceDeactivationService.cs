using IntegrationRequests;

namespace ReferalProgram.Application.Services;

public sealed class ExpiredFirstPlaceDeactivationService
{
    public const string DisabledMessage = "Expired first-place deactivation is temporarily disabled.";

    public Task ExecuteAsync(
        DeactivateExpiredFirstPlacesRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException(new NotSupportedException(DisabledMessage));
    }
}
