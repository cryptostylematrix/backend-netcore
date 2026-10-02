using System.Net;
using System.Net.Sockets;

namespace Contracts.Infrastructure.Metadata;

internal interface IJettonMetadataDocumentClient
{
    Task<JsonDocument> GetAsync(string uri, CancellationToken ct);
}

// Token-controlled URLs must not grant access to internal services or follow unchecked redirects.
internal sealed class JettonMetadataHttpClient : IJettonMetadataDocumentClient, IDisposable
{
    private const int MaxBytes = 262144;
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectCallback = ConnectPublicAsync,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    internal static Uri GetPublicUri(string value)
    {
        if (value.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
        {
            var path = value[7..];
            if (path.StartsWith("ipfs/", StringComparison.Ordinal)) path = path[5..];
            value = "https://ipfs.io/ipfs/" + path;
        }
        if (value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
            || !uri.IsDefaultPort || uri.IsLoopback)
            throw new HttpRequestException("Unsupported metadata URI.");
        if (IPAddress.TryParse(uri.DnsSafeHost, out var ip) && !IsPublicAddress(ip))
            throw new HttpRequestException("Metadata host must be public.");
        return uri;
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return !BlockedV4.Any(network => network.Contains(address));
        return GlobalV6.Contains(address) && !BlockedV6.Any(network => network.Contains(address));
    }

    private static readonly IPNetwork[] BlockedV4 = [
        IPNetwork.Parse("0.0.0.0/8"), IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"), IPNetwork.Parse("169.254.0.0/16"), IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.0.0.0/24"), IPNetwork.Parse("192.0.2.0/24"), IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("198.18.0.0/15"), IPNetwork.Parse("198.51.100.0/24"), IPNetwork.Parse("203.0.113.0/24"),
        IPNetwork.Parse("224.0.0.0/3")
    ];
    private static readonly IPNetwork GlobalV6 = IPNetwork.Parse("2000::/3");
    private static readonly IPNetwork[] BlockedV6 = [IPNetwork.Parse("2001::/23"), IPNetwork.Parse("2001:db8::/32"), IPNetwork.Parse("2002::/16")];

    private static async ValueTask<Stream> ConnectPublicAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var addresses = await System.Net.Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
        // Validate the resolved address and connect directly to it to prevent DNS rebinding.
        foreach (var address in addresses.Where(IsPublicAddress))
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException) { socket.Dispose(); }
            catch { socket.Dispose(); throw; }
        }
        throw new HttpRequestException("No reachable public metadata host.");
    }

    public async Task<JsonDocument> GetAsync(string uri, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var token = timeout.Token;
        var target = GetPublicUri(uri);
        for (var redirects = 0; redirects <= 3; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, target);
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                var location = response.Headers.Location ?? throw new HttpRequestException("Missing metadata redirect.");
                target = GetPublicUri(new Uri(target, location).AbsoluteUri);
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaxBytes) throw new HttpRequestException("Metadata is too large.");
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var bytes = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, token)) > 0)
            {
                if (bytes.Length + count > MaxBytes) throw new HttpRequestException("Metadata is too large.");
                bytes.Write(buffer, 0, count);
            }
            return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        }
        throw new HttpRequestException("Too many metadata redirects.");
    }

    public void Dispose() => client.Dispose();
}
