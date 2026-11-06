using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApodHome.Core;

public sealed class NasaApodProvider(HttpClient httpClient) : IApodProvider
{
    private static readonly Uri Endpoint = new("https://science.nasa.gov/wp-json/wp/v2/apod-basic");
    private static readonly Regex ImageUrl = new("""https?://[^"'\s<>]+?\.(?:jpe?g|png|webp)(?:\?[^"'\s<>]*)?""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OgImage = new("""<meta[^>]+(?:property|name)=["']og:image["'][^>]+content=["'](?<url>[^"']+)["']|<meta[^>]+content=["'](?<url>[^"']+)[^>]+(?:property|name)=["']og:image["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<ApodItem> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var request = new UriBuilder(Endpoint) { Query = $"date={date:yyyy-MM-dd}" }.Uri;
        using var response = await SafeHttp.GetAsync(httpClient, request, SafeHttp.IsNasaPage, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        var post = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().FirstOrDefault()
            : document.RootElement;

        if (post.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new InvalidDataException("NASA returned no APOD entry for this date.");

        var title = HtmlDecode(ReadText(post, "title") ?? "Astronomy Picture of the Day");
        var page = ReadText(post, "link") ?? "https://science.nasa.gov/apod/";
        var content = ReadText(post, "content") ?? string.Empty;
        var mediaType = content.Contains("youtube", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("vimeo", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("<iframe", StringComparison.OrdinalIgnoreCase)
            ? ApodMediaType.Video : ApodMediaType.Image;

        Uri pageUri = ToUri(page, "APOD page", SafeHttp.IsNasaPage);
        string? source = FindImageUrl(post) ?? FindImageUrl(content);
        if (mediaType == ApodMediaType.Video || source is null)
            source = await GetPreviewImageAsync(pageUri, cancellationToken).ConfigureAwait(false) ?? source;
        if (source is null)
            throw new InvalidDataException("The APOD entry does not contain a usable image or video preview.");

        return new ApodItem(date, title, pageUri, ToUri(WebUtility.HtmlDecode(source), "APOD media", SafeHttp.IsHttps), mediaType, HtmlDecode(content));
    }

    private async Task<string?> GetPreviewImageAsync(Uri page, CancellationToken cancellationToken)
    {
        using var response = await SafeHttp.GetAsync(httpClient, page, SafeHttp.IsNasaPage, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var preview = OgImage.Match(html).Groups["url"].Value;
        return !string.IsNullOrWhiteSpace(preview) ? preview : FindImageUrl(html);
    }

    private static string? FindImageUrl(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return FindImageUrl(element.GetString() ?? string.Empty);
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("media", StringComparison.OrdinalIgnoreCase) ||
                    property.Name is "acf" or "content")
                {
                    var found = FindImageUrl(property.Value);
                    if (found is not null) return found;
                }
            }
        }
        if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray())
            {
                var found = FindImageUrl(child);
                if (found is not null) return found;
            }
        return null;
    }

    public static string? FindImageUrl(string text)
    {
        var match = ImageUrl.Match(WebUtility.HtmlDecode(text));
        return match.Success ? match.Value : null;
    }

    private static string? ReadText(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return null;
        return property.ValueKind == JsonValueKind.Object && property.TryGetProperty("rendered", out var rendered)
            ? rendered.GetString()
            : property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static Uri ToUri(string value, string label, Func<Uri, bool> validator) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && validator(uri)
            ? uri : throw new InvalidDataException($"Invalid or unsafe {label} URL.");

    private static string HtmlDecode(string value) => WebUtility.HtmlDecode(Regex.Replace(value, "<.*?>", " "));
}
