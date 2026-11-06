using System.Net;
using System.Net.Sockets;

namespace ApodHome.Core;

public static class SafeHttp
{
    public const long MaxImageBytes = 30L * 1024 * 1024;
    public const long MaxPixels = 100_000_000;

    public static bool IsHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback &&
        !IPAddress.TryParse(uri.Host, out _) && string.IsNullOrEmpty(uri.UserInfo);

    public static bool IsNasaPage(Uri uri) =>
        IsHttps(uri) && (uri.Host.Equals("science.nasa.gov", StringComparison.OrdinalIgnoreCase) ||
                         uri.Host.EndsWith(".science.nasa.gov", StringComparison.OrdinalIgnoreCase));

    public static async Task<HttpResponseMessage> GetAsync(
        HttpClient client,
        Uri initialUri,
        Func<Uri, bool> allowedUri,
        CancellationToken cancellationToken)
    {
        var uri = initialUri;
        for (var redirects = 0; redirects <= 3; redirects++)
        {
            if (!allowedUri(uri)) throw new InvalidDataException($"Blocked unsafe download URL: {uri.Scheme}://{uri.Host}");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!IsRedirect(response.StatusCode)) return response;

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null) throw new InvalidDataException("Redirect response did not include a destination.");
            uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
        }
        throw new InvalidDataException("Too many redirects while retrieving APOD content.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or
        HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
}
