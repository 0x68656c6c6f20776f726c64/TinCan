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
        // Try to find settings.json by traversing up from test output directory
        var dir = Directory.GetCurrentDirectory();
        string? settingsPath = null;

        // Search up to 5 levels for settings.json
        for (int i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(dir, "settings.json");
            if (File.Exists(candidate))
            {
                settingsPath = candidate;
                break;
            }
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        // Also check environment variable
        var envKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY");

        if (!string.IsNullOrEmpty(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                _apiKey = settings?.Providers?.Finnhub?.ApiKey;
            }
            catch { }
        }

        // Fallback to environment variable
        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your_finnhub_api_key")
        {
            _apiKey = envKey;
        }
    }

    [TestMethod]
    public async Task FetchPriceAsync_UnitySymbol_ReturnsValidPrice()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured - set FINNHUB_API_KEY env or add settings.json at repo root");

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
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured - set FINNHUB_API_KEY env or add settings.json at repo root");

        var service = new FinnhubService(_apiKey, 10);
        var result = await service.FetchPriceAsync("AAPL");

        Assert.IsNotNull(result);
        Assert.AreEqual("AAPL", result.Symbol);
        Assert.IsTrue(result.Price > 0);
    }

    [TestMethod]
    public async Task FetchPriceAsync_InvalidSymbol_ReturnsNull()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured - set FINNHUB_API_KEY env or add settings.json at repo root");

        var service = new FinnhubService(_apiKey, 10);
        var result = await service.FetchPriceAsync("INVALID_SYMBOL_XYZ");

        // Invalid symbols typically return 0 price
        Assert.IsNull(result);
    }
}
