using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;
using TinCan.Strategies;
using TinCan.Tests.Integration.Helpers;

namespace TinCan.Tests.Integration;

[TestClass]
public class OpenClawStrategyIntegrationTests
{
    private string? _apiKey;

    [TestInitialize]
    public void Setup()
    {
        _apiKey = FinnhubServiceSetupHelper.SetupAndGetApiKey();
    }

    [TestMethod]
    public async Task GenerateAsync_WithRealFinnhubData_ReturnsValidSignal()
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
        var signal = await strategy.GenerateAsync(context);

        // Assert: Signal is valid
        Assert.IsNotNull(signal);
        Assert.IsTrue(Enum.IsDefined(typeof(SignalType), signal.Type), "Invalid SignalType");
        Assert.IsTrue(signal.Confidence >= 0.0 && signal.Confidence <= 1.0, "Confidence out of range");
        Assert.IsFalse(string.IsNullOrEmpty(signal.Reason), "Signal reason should not be empty");

        Console.WriteLine($"Signal generated: {signal.Type} (confidence: {signal.Confidence})");
        Console.WriteLine($"Reason: {signal.Reason}");
    }
}
