using ApodHome.Core;
using System.Net.Http;
using Xunit;

namespace ApodHome.Core.Tests;

public sealed class ApodProviderTests
{
    [Fact]
    public async Task Uses_current_nasa_fields_for_the_requested_apod()
    {
        const string response = """
            [{
              "date":"2026-11-07",
              "title":"De Gerlache-Kocher Massif",
              "url":"https://science.nasa.gov/image-article/apod-2026-november-7-de-gerlache-kocher-massif/",
              "media_type":"image",
              "hdurl":"https://assets.science.nasa.gov/dynamicimage/apod/GerlacheKocherMassif_Sans.jpg?w=1500&h=1022",
              "basic_html":"<meta property='og:image' content='https://science.nasa.gov/generic-preview.jpg'>",
              "explanation":"The actual APOD."
            }]
            """;
        using var client = new HttpClient(new StubHandler(response));
        var provider = new NasaApodProvider(client);

        var item = await provider.GetAsync(new DateOnly(2026, 11, 7));

        Assert.Equal("De Gerlache-Kocher Massif", item.Title);
        Assert.Equal("https://science.nasa.gov/image-article/apod-2026-november-7-de-gerlache-kocher-massif/", item.PageUrl.AbsoluteUri);
        Assert.Equal("https://assets.science.nasa.gov/dynamicimage/apod/GerlacheKocherMassif_Sans.jpg?w=3840&fit=clip", item.MediaUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData("<img src='https://images.nasa.gov/example.jpg'>", "https://images.nasa.gov/example.jpg")]
    [InlineData("https://images.nasa.gov/example.webp?size=large", "https://images.nasa.gov/example.webp?size=large")]
    public void Finds_supported_image_urls(string source, string expected)
    {
        Assert.Equal(expected, NasaApodProvider.FindImageUrl(source));
    }

    [Fact]
    public void Ignores_text_without_an_image_url()
    {
        Assert.Null(NasaApodProvider.FindImageUrl("A beautiful astronomy video."));
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
    }
}
