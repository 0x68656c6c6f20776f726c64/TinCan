using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Strategies;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Integration;

[TestClass]
public class OpenClawStrategyIntegrationTests
{
    private string? _apiKey;
    private OpenClawService? _openClawService;

    [TestInitialize]
    public void Setup()
    {
        // Try to find settings.json by traversing up from test output directory
        var dir = Directory.GetCurrentDirectory();
        string? settingsPath = null;

        for (int i = 0; i < 5; i++)
        {
            var rootCandidate = Path.Combine(dir, "settings.json");
            if (File.Exists(rootCandidate))
            {
                settingsPath = rootCandidate;
                break;
            }
            var stockBotCandidate = Path.Combine(dir, "stock_bot", "settings.json");
            if (File.Exists(stockBotCandidate))
            {
                settingsPath = stockBotCandidate;
                break;
            }
            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        if (settingsPath != null && File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                dynamic? settings = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                if (settings?.providers?.finnhub?.api_key != null)
                {
                    _apiKey = settings.providers.finnhub.api_key;
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        // Get OpenClaw gateway token from environment
        var gatewayToken = Environment.GetEnvironment("OPENCLAW_GATEWAY_TOKEN");
        if (string.IsNullOrEmpty(gatewayToken))
        {
            throw new InvalidOperationException("OPENCLAW_GATEWAY_TOKEN environment variable is required for integration tests");
        }
        
        _openClawService = new OpenClawService(gatewayToken);
    }

    [TestMethod]
    public async Task OpenClawStrategy_Generate_WithRealData_ReturnsValidSignal()
    {
        // Skip if no API key available
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("Finnhub API key not configured");
            return;
        }

        // Arrange
        var strategy = new OpenClawSimpleStrategy(_openClawService!);
        
        // Get real market data from Finnhub
        var finnhubService = new FinnhubService(_apiKey!);
        var price = await finnhubService.FetchPriceAsync("AAPL");
        
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = price
        };

        // Act
        var signal = strategy.Generate(context);

        // Assert
        Assert.IsNotNull(signal);
        Assert.IsTrue(
            signal.Type == SignalType.Buy || 
            signal.Type == SignalType.Sell || 
            signal.Type == SignalType.Hold,
            $"Signal type should be Buy, Sell, or Hold, got {signal.Type}");
        Assert.IsTrue(signal.Confidence >= 0.0 && signal.Confidence <= 1.0,
            $"Confidence should be between 0 and 1, got {signal.Confidence}");
        Assert.IsFalse(string.IsNullOrEmpty(signal.Reason),
            "Reason should not be empty");
    }

    [TestMethod]
    public async Task OpenClawService_GetStrategySuggestion_WithRealOpenClaw_ReturnsValidResult()
    {
        // This test verifies OpenClaw can process a market context and return strategy suggestion
        
        if (_openClawService == null)
        {
            Assert.Inconclusive("OpenClaw service not initialized");
            return;
        }

        var context = new MarketContext
        {
            Symbol = "TSLA",
            CurrentPrice = new StockPrice 
            { 
                Symbol = "TSLA", 
                Price = 250.00,
                High = 255.00,
                Low = 248.00,
                Timestamp = DateTime.UtcNow
            }
        };

        var result = await _openClawService.GetStrategySuggestionAsync(context);

        Assert.IsNotNull(result, "OpenClaw should return a result");
        Assert.IsTrue(
            result!.Suggestion == "buy" || 
            result.Suggestion == "sell" || 
            result.Suggestion == "hold",
            $"Suggestion should be buy, sell, or hold, got {result.Suggestion}");
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.IsFalse(string.IsNullOrEmpty(result.Reason));
    }
}
