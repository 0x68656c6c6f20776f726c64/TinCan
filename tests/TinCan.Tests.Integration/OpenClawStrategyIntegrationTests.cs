using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;
using TinCan.Strategies;
using Newtonsoft.Json;

namespace TinCan.Tests.Integration;

[TestClass]
public class OpenClawStrategyIntegrationTests
{
    private string? _apiKey;

    [TestInitialize]
    public void Setup()
    {
        // Try to find settings.json by traversing up from test output directory
        var dir = Directory.GetCurrentDirectory();
        string? settingsPath = null;

        // Search up to 5 levels for settings.json in repo root or stock_bot/
        for (int i = 0; i < 5; i++)
        {
            // Check root settings.json
            var rootCandidate = Path.Combine(dir, "settings.json");
            if (File.Exists(rootCandidate))
            {
                settingsPath = rootCandidate;
                break;
            }
            // Check stock_bot/settings.json (where it lives)
            var botCandidate = Path.Combine(dir, "stock_bot", "settings.json");
            if (File.Exists(botCandidate))
            {
                settingsPath = botCandidate;
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
    public async Task Generate_WithRealFinnhubData_ReturnsValidSignal()
    {
        if (string.IsNullOrEmpty(_apiKey)) 
            Assert.Inconclusive("API key not configured - set FINNHUB_API_KEY env or add settings.json");

        // Arrange: Fetch real data from Finnhub
        var finnhubService = new FinnhubService(_apiKey, 10);
        var stockPrice = await finnhubService.FetchPriceAsync("AAPL");
        
        Assert.IsNotNull(stockPrice, "Failed to fetch stock price from Finnhub");
        Assert.IsTrue(stockPrice.Price > 0, "Invalid stock price received");

        // Arrange: Create market context
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = stockPrice
        };

        // Arrange: Create OpenClaw service and strategy
        var openClawService = new OpenClawService("http://localhost:18789", "");
        var strategy = new OpenClawStrategy(openClawService);

        // Act: Generate signal
        var signal = strategy.Generate(context);

        // Assert: Signal is valid
        Assert.IsNotNull(signal);
        Assert.IsTrue(Enum.IsDefined(typeof(SignalType), signal.Type), "Invalid SignalType");
        Assert.IsTrue(signal.Confidence >= 0.0 && signal.Confidence <= 1.0, "Confidence out of range");
        Assert.IsFalse(string.IsNullOrEmpty(signal.Reason), "Signal reason should not be empty");

        Console.WriteLine($"Signal generated: {signal.Type} (confidence: {signal.Confidence})");
        Console.WriteLine($"Reason: {signal.Reason}");
    }

    [TestMethod]
    public async Task Generate_WithRealFinnhubData_OpenClawSimpleStrategy_ReturnsValidSignal()
    {
        if (string.IsNullOrEmpty(_apiKey)) 
            Assert.Inconclusive("API key not configured - set FINNHUB_API_KEY env or add settings.json");

        // Arrange: Fetch real data from Finnhub
        var finnhubService = new FinnhubService(_apiKey, 10);
        var stockPrice = await finnhubService.FetchPriceAsync("U");
        
        Assert.IsNotNull(stockPrice, "Failed to fetch stock price from Finnhub");
        Assert.IsTrue(stockPrice.Price > 0, "Invalid stock price received");

        // Arrange: Create market context
        var context = new MarketContext
        {
            Symbol = "U",
            CurrentPrice = stockPrice
        };

        // Arrange: Create OpenClaw service and simple strategy
        var openClawService = new OpenClawService("http://localhost:18789", "");
        var strategy = new OpenClawSimpleStrategy(openClawService);

        // Act: Generate signal
        var signal = strategy.Generate(context);

        // Assert: Signal is valid
        Assert.IsNotNull(signal);
        Assert.IsTrue(Enum.IsDefined(typeof(SignalType), signal.Type), "Invalid SignalType");
        Assert.IsTrue(signal.Confidence >= 0.0 && signal.Confidence <= 1.0, "Confidence out of range");
        Assert.IsFalse(string.IsNullOrEmpty(signal.Reason), "Signal reason should not be empty");

        Console.WriteLine($"OpenClawSimpleStrategy Signal: {signal.Type} (confidence: {signal.Confidence})");
        Console.WriteLine($"Reason: {signal.Reason}");
    }
}
