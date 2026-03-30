using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;
using TinCan.Strategies;

namespace TinCan.Tests.Integration;

[TestClass]
public class SignalExecutorIntegrationTests
{
    private string _apiKey = "";
    private string? _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _apiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY") ?? "";
        if (string.IsNullOrEmpty(_apiKey))
        {
            var settingsPath = Path.Combine(GetTinCanDir(), "settings.json");
            if (File.Exists(settingsPath))
            {
                var content = File.ReadAllText(settingsPath);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"""ApiKey"":\s*""([^""]+)""");
                if (match.Success)
                    _apiKey = match.Groups[1].Value;
            }
        }

        _tempDir = Path.Combine(Path.GetTempPath(), $"tincan_exec_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempDir != null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static string GetTinCanDir()
    {
        var testDir = Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
    }

    [TestMethod]
    public async Task FullFlow_StrategyToOrderToFill_UpdatesPosition()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // Arrange - Create real Finnhub service
        var finnhub = new FinnhubService(_apiKey, 10);
        var broker = new PaperBrokerService(finnhub, _tempDir, 10000.00);

        // Create a mock strategy that generates Buy signal
        var mockStrategy = new Mock<IStrategy>();
        mockStrategy.Setup(s => s.Name).Returns("TestStrategy");
        mockStrategy.Setup(s => s.GenerateAsync(It.IsAny<MarketContext>()))
            .ReturnsAsync(new Signal
            {
                Type = SignalType.Buy,
                Symbol = "U",
                Quantity = 1,
                Reason = "Test buy signal",
                Confidence = 1.0
            });

        var executor = new SignalExecutor(mockStrategy.Object, broker, finnhub);

        // Create a mock market context
        var context = new MarketContext
        {
            Symbol = "U",
            PriceHistory = new List<StockPrice>
            {
                new StockPrice { Symbol = "U", Price = 100, High = 101, Low = 99 }
            },
            CurrentPrice = new StockPrice { Symbol = "U", Price = 100, High = 101, Low = 99 }
        };

        // Act - Execute signal
        var signal = await mockStrategy.Object.GenerateAsync(context);
        signal.Symbol = "U";
        var result = await executor.ExecuteAsync(signal);

        // Assert - Verify order was placed and filled
        Assert.IsTrue(result.Success, $"Execution failed: {result.ErrorMessage}");
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderStatus.Filled, result.Order.Status);
        Assert.AreEqual(OrderSide.Buy, result.Order.Side);

        // Verify balance updated
        var balance = await broker.GetBalanceAsync("U");
        Assert.IsTrue(balance.Cash < 10000.00, "Cash should have decreased after buy");
    }

    [TestMethod]
    public async Task FullFlow_HoldSignal_NoOrderPlaced()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // Arrange
        var finnhub = new FinnhubService(_apiKey, 10);
        var broker = new PaperBrokerService(finnhub, _tempDir, 10000.00);

        var mockStrategy = new Mock<IStrategy>();
        mockStrategy.Setup(s => s.Name).Returns("TestStrategy");
        mockStrategy.Setup(s => s.GenerateAsync(It.IsAny<MarketContext>()))
            .ReturnsAsync(new Signal
            {
                Type = SignalType.Hold,
                Symbol = "U",
                Quantity = 100,
                Reason = "No action needed",
                Confidence = 1.0
            });

        var executor = new SignalExecutor(mockStrategy.Object, broker, finnhub);

        var context = new MarketContext
        {
            Symbol = "U",
            PriceHistory = new List<StockPrice>(),
            CurrentPrice = null
        };

        // Act
        var signal = await mockStrategy.Object.GenerateAsync(context);
        signal.Symbol = "U";
        var result = await executor.ExecuteAsync(signal);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNull(result.Order);
    }

    [TestMethod]
    public async Task FullFlow_SellSignal_UpdatesPositionAndCash()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // Arrange
        var finnhub = new FinnhubService(_apiKey, 10);
        var broker = new PaperBrokerService(finnhub, _tempDir, 10000.00);

        // First buy some shares
        await broker.PlaceOrderAsync("U", 10, OrderSide.Buy, OrderType.Market);

        var initialBalance = await broker.GetBalanceAsync("U");
        var initialCash = initialBalance.Cash;

        // Create strategy that sells
        var mockStrategy = new Mock<IStrategy>();
        mockStrategy.Setup(s => s.Name).Returns("TestStrategy");
        mockStrategy.Setup(s => s.GenerateAsync(It.IsAny<MarketContext>()))
            .ReturnsAsync(new Signal
            {
                Type = SignalType.Sell,
                Symbol = "U",
                Quantity = 5,
                Reason = "Take profit",
                Confidence = 1.0
            });

        var executor = new SignalExecutor(mockStrategy.Object, broker, finnhub);

        var context = new MarketContext
        {
            Symbol = "U",
            PriceHistory = new List<StockPrice>(),
            CurrentPrice = null
        };

        // Act
        var signal = await mockStrategy.Object.GenerateAsync(context);
        signal.Symbol = "U";
        var result = await executor.ExecuteAsync(signal);

        // Assert
        Assert.IsTrue(result.Success, $"Sell execution failed: {result.ErrorMessage}");
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderSide.Sell, result.Order.Side);

        var finalBalance = await broker.GetBalanceAsync("U");
        Assert.IsTrue(finalBalance.Cash > initialCash, "Cash should increase after selling");
    }
}
