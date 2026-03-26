using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Integration;

[TestClass]
public class FinnhubServiceIntegrationTests
{
    private string? _apiKey;

    [TestInitialize]
    public void Setup()
    {
        // API key should be set via settings or environment variable in CI
        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig
                {
                    ApiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY") ?? "d6u58qpr01qp1k9ba1sgd6u58qpr01qp1k9ba1t0",
                    Timeout = 10
                }
            }
        };
        _apiKey = settings.Providers?.Finnhub?.ApiKey;
    }

    [TestMethod]
    public async Task FetchPriceAsync_UnitySymbol_ReturnsValidPrice()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var service = new FinnhubService(_apiKey, 10);
        var result = await service.FetchPriceAsync("U");

        Assert.IsNotNull(result);
        Assert.AreEqual("U", result.Symbol);
        Assert.IsTrue(result.Price > 0);
        Assert.IsTrue(result.High >= result.Price);
        Assert.IsTrue(result.Low <= result.Price);
    }

    [TestMethod]
    public async Task FetchPriceAsync_AAPL_ReturnsValidPrice()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var service = new FinnhubService(_apiKey, 10);
        var result = await service.FetchPriceAsync("AAPL");

        Assert.IsNotNull(result);
        Assert.AreEqual("AAPL", result.Symbol);
        Assert.IsTrue(result.Price > 0);
    }

    [TestMethod]
    public async Task FetchPriceAsync_InvalidSymbol_ReturnsNull()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var service = new FinnhubService(_apiKey, 10);
        var result = await service.FetchPriceAsync("INVALID_SYMBOL_XYZ");

        // Invalid symbols typically return 0 price
        Assert.IsNull(result);
    }
}
