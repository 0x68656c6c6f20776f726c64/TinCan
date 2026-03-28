using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Strategies;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class OpenClawSimpleStrategyTests
{
    [TestMethod]
    public void Name_ReturnsCorrectName()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        var strategy = new OpenClawSimpleStrategy(mockService.Object);

        // Assert
        Assert.AreEqual("OpenClawSimpleStrategy", strategy.Name);
    }

    [TestMethod]
    public async Task GenerateAsync_WithBuySuggestion_ReturnsBuySignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResponse
            {
                Suggestion = "buy",
                Confidence = 0.82,
                Reason = "trend+momentum"
            });

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(SignalType.Buy, signal.Type);
        Assert.AreEqual(0.82, signal.Confidence);
        Assert.AreEqual("trend+momentum", signal.Reason);
    }

    [TestMethod]
    public async Task GenerateAsync_WithSellSuggestion_ReturnsSellSignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResponse
            {
                Suggestion = "sell",
                Confidence = 0.75,
                Reason = "overbought"
            });

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(SignalType.Sell, signal.Type);
        Assert.AreEqual(0.75, signal.Confidence);
        Assert.AreEqual("overbought", signal.Reason);
    }

    [TestMethod]
    public async Task GenerateAsync_WithHoldSuggestion_ReturnsHoldSignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResponse
            {
                Suggestion = "hold",
                Confidence = 0.5,
                Reason = "no clear trend"
            });

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual(0.5, signal.Confidence);
        Assert.AreEqual("no clear trend", signal.Reason);
    }

    [TestMethod]
    public async Task GenerateAsync_ConfidenceClampedToValidRange()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResponse
            {
                Suggestion = "buy",
                Confidence = 1.5, // Over 1.0
                Reason = "test"
            });

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(1.0, signal.Confidence); // Clamped to 1.0
    }

    [TestMethod]
    public async Task GenerateAsync_InheritsFromOpenClawStrategy()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        var strategy = new OpenClawSimpleStrategy(mockService.Object);

        // Assert - inherits GenerateAsync implementation from OpenClawStrategy
        Assert.IsInstanceOfType(strategy, typeof(OpenClawStrategy));
    }
}
