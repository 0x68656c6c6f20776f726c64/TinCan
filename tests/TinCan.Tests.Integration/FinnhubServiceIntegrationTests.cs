using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;
using Newtonsoft.Json;

namespace TinCan.Tests.Integration;

[TestClass]
public class FinnhubServiceIntegrationTests
{
    private string? _apiKey;

    [TestInitialize]
    public void Setup()
    {
        // Load API key from settings.json
        try
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                _apiKey = settings?.Providers?.Finnhub?.ApiKey;
            }
        }
        catch
        {
            // Fallback
        }

        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your_finnhub_api_key")
        {
            _apiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY");
        }
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
