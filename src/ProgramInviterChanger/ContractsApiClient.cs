using System.Net.Http.Json;
using Contracts.Dto;

namespace ProgramInviterChanger;

internal sealed class ContractsApiClient(HttpClient httpClient, TimeSpan requestDelay)
{
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public async Task<string> GetProfileAddressAsync(
        string login,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_nextRequestAt > now)
            await Task.Delay(_nextRequestAt - now, cancellationToken);

        _nextRequestAt = DateTimeOffset.UtcNow + requestDelay;
        var path = $"contracts/profile-collection/nft-addr-by-login/{Uri.EscapeDataString(login)}";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The contracts API could not resolve profile login '{login}' "
                + $"(HTTP {(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<NftAddressResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(result?.Addr))
            throw new InvalidOperationException($"Profile login '{login}' returned no address.");

        return result.Addr.Trim();
    }
}
