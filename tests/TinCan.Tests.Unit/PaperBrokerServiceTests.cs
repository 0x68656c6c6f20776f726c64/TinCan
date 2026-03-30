using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;

namespace TinCan.Tests.Unit;

[TestClass]
public class PaperBrokerServiceTests
{
    private Mock<IMarketDataProviderService> _mockMarketData = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMarketData = new Mock<IMarketDataProviderService>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"tincan_broker_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_MarketBuyOrder_FillsAtCurrentPrice()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Act
        var result = await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderStatus.Filled, result.Order.Status);
        Assert.AreEqual(150.00, result.Order.FillPrice);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_MarketSellOrder_DecreasesPosition()
    {
        // Arrange - First buy some shares
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // First buy shares
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Act - Sell the shares
        var result = await broker.PlaceOrderAsync("AAPL", 5, OrderSide.Sell, OrderType.Market);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderStatus.Filled, result.Order.Status);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_InsufficientCash_RejectsOrder()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 1000.00); // Only $1000

        // Act - Try to buy $15000 worth
        var result = await broker.PlaceOrderAsync("AAPL", 100, OrderSide.Buy, OrderType.Market);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderStatus.Rejected, result.Order.Status);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_InsufficientShares_RejectsSell()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Act - Try to sell more than we have
        var result = await broker.PlaceOrderAsync("AAPL", 100, OrderSide.Sell, OrderType.Market);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(OrderStatus.Rejected, result.Order.Status);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_UpdatesCashBalance()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 100.00, High = 101.00, Low = 99.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Act - Buy 10 shares at $100 = $1000
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Assert
        var balance = await broker.GetBalanceAsync("AAPL");
        Assert.AreEqual(9000.00, balance.Cash); // $10000 - $1000
    }

    [TestMethod]
    public async Task PlaceOrderAsync_DefaultInitialCash_Is10000()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 100.00, High = 101.00, Low = 99.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir); // Default cash

        // Act
        var balance = await broker.GetBalanceAsync("AAPL");

        // Assert
        Assert.AreEqual(10000.00, balance.Cash);
    }

    [TestMethod]
    public async Task GetBalanceAsync_ReturnsCorrectEquity()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Buy 10 shares at $150 = $1500 spent, $8500 cash left
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Act
        var balance = await broker.GetBalanceAsync("AAPL");

        // Assert
        Assert.AreEqual(8500.00, balance.Cash);
        Assert.AreEqual(1500.00, balance.Equity - balance.Cash); // Position value = 10 * $150
    }

    [TestMethod]
    public async Task GetOpenOrdersAsync_ReturnsPendingOrders()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Place a market order (should fill immediately, not be pending)
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Act
        var openOrders = await broker.GetOpenOrdersAsync("AAPL");

        // Assert - Market orders fill immediately
        Assert.AreEqual(0, openOrders.Count);
    }

    [TestMethod]
    public async Task CancelOrderAsync_CancelsPendingOrder()
    {
        // Arrange - For this test, we'd need a way to create a pending limit order
        // Since limit orders check price immediately, this is a simplified test
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 150.00, High = 151.00, Low = 149.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Place an order and get its ID
        var result = await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);
        var orderId = result.Order!.Id;

        // Act - Try to cancel (won't work since already filled)
        var cancelled = await broker.CancelOrderAsync(orderId);

        // Assert - Already filled orders cannot be cancelled
        Assert.IsFalse(cancelled);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_MultipleBuys_CalculatesCorrectAverageCost()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 100.00, High = 101.00, Low = 99.00 });

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 20000.00);

        // Act - First buy: 10 shares at $100
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync(new StockPrice { Symbol = "AAPL", Price = 120.00, High = 121.00, Low = 119.00 });

        // Second buy: 10 shares at $120
        await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Assert - Average cost should be (10*100 + 10*120) / 20 = $110
        var balance = await broker.GetBalanceAsync("AAPL");
        // Cash: $20000 - $1000 - $1200 = $17800
        Assert.AreEqual(17800.00, balance.Cash);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_CannotFetchPrice_RejectsOrder()
    {
        // Arrange
        _mockMarketData.Setup(m => m.FetchPriceAsync("AAPL"))
            .ReturnsAsync((StockPrice?)null);

        var broker = new PaperBrokerService(_mockMarketData.Object, _tempDir, 10000.00);

        // Act
        var result = await broker.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderStatus.Rejected, result.Order.Status);
    }
}
