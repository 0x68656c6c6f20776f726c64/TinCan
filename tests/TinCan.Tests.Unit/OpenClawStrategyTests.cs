using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Strategies;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class OpenClawStrategyTests
{
    [TestMethod]
    public void Generate_WithBuySuggestion_ReturnsBuySignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResult
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
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(SignalType.Buy, signal.Type);
        Assert.AreEqual(0.82, signal.Confidence);
        Assert.AreEqual("trend+momentum", signal.Reason);
    }

    [TestMethod]
    public void Generate_WithSellSuggestion_ReturnsSellSignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResult
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
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(SignalType.Sell, signal.Type);
        Assert.AreEqual(0.75, signal.Confidence);
        Assert.AreEqual("overbought", signal.Reason);
    }

    [TestMethod]
    public void Generate_WithHoldSuggestion_ReturnsHoldSignal()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResult
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
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual(0.5, signal.Confidence);
        Assert.AreEqual("no clear trend", signal.Reason);
    }

    [TestMethod]
    public void Generate_WhenOpenClawReturnsNull_ReturnsHoldWithLowConfidence()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenClawResult?)null);

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual(0.0, signal.Confidence);
        Assert.AreEqual("OpenClaw returned no result", signal.Reason);
    }

    [TestMethod]
    public void Generate_WhenOpenClawThrowsException_ReturnsHoldWithErrorReason()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual(0.0, signal.Confidence);
        Assert.IsTrue(signal.Reason.Contains("Connection failed"));
    }

    [TestMethod]
    public void Generate_ConfidenceClampedToValidRange()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenClawResult
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
        var signal = strategy.Generate(context);

        // Assert
        Assert.AreEqual(1.0, signal.Confidence); // Clamped to 1.0
    }

    [TestMethod]
    public void Name_ReturnsCorrectName()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        var strategy = new OpenClawSimpleStrategy(mockService.Object);

        // Assert
        Assert.AreEqual("OpenClawSimpleStrategy", strategy.Name);
    }
}
