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
    public void Name_ReturnsCorrectName()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        var strategy = new OpenClawStrategy(mockService.Object);

        // Assert
        Assert.AreEqual("OpenClawStrategy", strategy.Name);
    }

    [TestMethod]
    public async Task GenerateAsync_WithNullResponse_ReturnsHoldWithNoResponseMessage()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenClawResponse?)null);

        var strategy = new OpenClawStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No response from OpenClaw", signal.Reason);
        Assert.AreEqual(0.0, signal.Confidence);
    }

    [TestMethod]
    public async Task GenerateAsync_WhenServiceThrows_ReturnsHoldWithErrorReason()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        mockService.Setup(s => s.GetStrategySuggestionAsync(It.IsAny<MarketContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        var strategy = new OpenClawStrategy(mockService.Object);
        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        // Act
        var signal = await strategy.GenerateAsync(context);

        // Assert
        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.IsTrue(signal.Reason.Contains("Connection failed"));
    }

    [TestMethod]
    public void InheritsFromStrategyBase()
    {
        // Arrange
        var mockService = new Mock<IOpenClawService>();
        var strategy = new OpenClawStrategy(mockService.Object);

        // Assert
        Assert.IsInstanceOfType(strategy, typeof(StrategyBase));
    }
}
