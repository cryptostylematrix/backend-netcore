using System.Net;
using System.Net.Http.Json;
using Contracts.Dto;

namespace ProgramMigrator;

internal sealed class ContractApiClient(
    HttpClient httpClient,
    TimeSpan requestDelay)
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16)
    ];

    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public Task<NftAddressResponse> GetProfileAddressByLoginAsync(
        string login,
        CancellationToken cancellationToken) =>
        GetAsync<NftAddressResponse>(
            $"contracts/profile-collection/nft-addr-by-login/{Encode(login)}",
            cancellationToken);

    public Task<ProfileProgramsResponse> GetProfileProgramsAsync(
        string profileAddr,
        CancellationToken cancellationToken) =>
        GetAsync<ProfileProgramsResponse>(
            $"contracts/profile-item/{Encode(profileAddr)}/programs",
            cancellationToken);

    public Task<ProfileDataResponse> GetProfileDataAsync(
        string profileAddr,
        CancellationToken cancellationToken) =>
        GetAsync<ProfileDataResponse>(
            $"contracts/profile-item/{Encode(profileAddr)}/nft-data",
            cancellationToken);

    public Task<InviteDataResponse> GetInviteDataAsync(
        string inviteAddr,
        CancellationToken cancellationToken) =>
        GetAsync<InviteDataResponse>(
            $"contracts/invite/{Encode(inviteAddr)}/data",
            cancellationToken);

    public Task<InviteAddressResponse> GetInviteAddressAsync(
        string inviteAddr,
        uint sequenceNumber,
        CancellationToken cancellationToken) =>
        GetAsync<InviteAddressResponse>(
            $"contracts/invite/{Encode(inviteAddr)}/invite-addr-by-seq-no/{sequenceNumber}",
            cancellationToken);

    private async Task<T> GetAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            await WaitForRequestSlotAsync(cancellationToken);

            try
            {
                using var response = await httpClient.GetAsync(relativePath, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                        ?? throw new InvalidOperationException(
                            $"Contracts API returned an empty response for '{relativePath}'.");
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = new HttpRequestException(
                    $"Contracts API returned {(int)response.StatusCode} for '{relativePath}': {responseBody}",
                    null,
                    response.StatusCode);

                if (!ShouldRetry(response.StatusCode) || attempt == RetryDelays.Length)
                    throw error;

                lastException = error;
            }
            catch (HttpRequestException exception) when (
                attempt < RetryDelays.Length
                && (exception.StatusCode is null || ShouldRetry(exception.StatusCode.Value)))
            {
                lastException = exception;
            }

            await Task.Delay(RetryDelays[attempt], cancellationToken);
        }

        throw lastException
            ?? new InvalidOperationException($"Contracts API request '{relativePath}' failed.");
    }

    private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_nextRequestAt > now)
            await Task.Delay(_nextRequestAt - now, cancellationToken);

        _nextRequestAt = DateTimeOffset.UtcNow + requestDelay;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout
        || (int)statusCode == 429
        || (int)statusCode >= 500;

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
