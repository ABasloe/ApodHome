using ApodHome.Core;
using Xunit;

namespace ApodHome.Core.Tests;

public sealed class ApodProviderTests
{
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
}
