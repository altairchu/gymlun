using System.Net;
using System.Net.Sockets;
using FoodDiary.API.Features.ProductPhotos;
using ModelContextProtocol;

namespace FoodDiary.API.Mcp.Tools.Products;

/// <summary>
/// Fetches an image from a URL an AI app passed in, which must not reach the server's own network
/// </summary>
public class PhotoDownloader(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "PhotoDownloads";

    public static void AddHttpClient(IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Checked at connect time, so a redirect or a DNS answer that changes cannot reach a private address
                ConnectCallback = ConnectToPublicAddress,
                MaxAutomaticRedirections = 3
            });
    }

    public async Task<MemoryStream> Download(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new McpException("The photo URL must be an absolute https URL.");
        }

        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new McpException($"The photo could not be downloaded: the server answered {(int)response.StatusCode}.");
            }

            if (response.Content.Headers.ContentLength > ProductPhotoProcessor.MaxUploadBytes)
            {
                throw TooLarge();
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            var photo = new MemoryStream();
            var chunk = new byte[81920];
            int read;

            while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (photo.Length + read > ProductPhotoProcessor.MaxUploadBytes)
                {
                    throw TooLarge();
                }

                photo.Write(chunk, 0, read);
            }

            photo.Position = 0;
            return photo;
        }
        catch (HttpRequestException)
        {
            throw new McpException("The photo could not be downloaded. Check that the URL is public and points to an image.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpException("The photo took too long to download.");
        }
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var first = address.GetAddressBytes()[0];

            // Unique local fc00::/7 alongside link and site local
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && (first & 0xFE) != 0xFC;
        }

        var bytes = address.GetAddressBytes();

        return bytes switch
        {
            [0, ..] => false,
            [10, ..] => false,
            [100, >= 64 and <= 127, ..] => false,
            [169, 254, ..] => false,
            [172, >= 16 and <= 31, ..] => false,
            [192, 168, ..] => false,
            [192, 0, 0, _] => false,
            [198, 18 or 19, ..] => false,
            [>= 224, ..] => false,
            _ => true
        };
    }

    private static McpException TooLarge() =>
        new($"The photo must be no larger than {ProductPhotoProcessor.MaxUploadBytes / 1024 / 1024} MB.");

    private static async ValueTask<Stream> ConnectToPublicAddress(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

        if (addresses.Length == 0 || !addresses.All(IsPublic))
        {
            throw new HttpRequestException($"{context.DnsEndPoint.Host} is not a public host.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
