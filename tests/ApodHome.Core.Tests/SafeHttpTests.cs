using ApodHome.Core;
using Xunit;

namespace ApodHome.Core.Tests;

public sealed class SafeHttpTests
{
    [Theory]
    [InlineData("https://science.nasa.gov/apod/", true)]
    [InlineData("https://images.nasa.gov/asset.jpg", true)]
    [InlineData("http://science.nasa.gov/apod/", false)]
    [InlineData("file:///C:/image.jpg", false)]
    [InlineData("https://127.0.0.1/image.jpg", false)]
    [InlineData("https://192.168.1.1/image.jpg", false)]
    public void Https_validator_rejects_local_and_non_https_urls(string value, bool expected)
    {
        Assert.Equal(expected, SafeHttp.IsHttps(new Uri(value)));
    }

    [Theory]
    [InlineData("https://science.nasa.gov/apod/", true)]
    [InlineData("https://cdn.science.nasa.gov/image.jpg", true)]
    [InlineData("https://science.nasa.gov.attacker.test/apod/", false)]
    [InlineData("https://www.nasa.gov/apod/", false)]
    public void Nasa_page_validator_allows_only_nasa_science_hosts(string value, bool expected)
    {
        Assert.Equal(expected, SafeHttp.IsNasaPage(new Uri(value)));
    }
}
