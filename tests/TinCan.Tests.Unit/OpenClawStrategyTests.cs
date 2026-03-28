using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Models;
using TinCan.Services;
using TinCan.Strategies;

namespace TinCan.Tests.Unit;

[TestClass]
public class OpenClawStrategyTests
{
    [TestMethod]
    public void Name_ReturnsCorrectName()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawStrategy(mockService.Object);
        Assert.AreEqual("OpenClawStrategy", strategy.Name);
    }

        [TestMethod]
    public async Task GenerateAsync_WithNullCurrentPrice_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawStrategy(mockService.Object);

        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = null
        };

        var signal = await strategy.GenerateAsync(context);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No current price available", signal.Reason);
    }

    [TestMethod]
    public async Task BuildSignalFromResponseAsync_WhenResponseIsNull_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new TestableOpenClawStrategy(mockService.Object);

        var signal = await strategy.CallBuildSignalFromResponseAsync(null);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No response from OpenClaw", signal.Reason);
        Assert.AreEqual(0.1, signal.Confidence);
    }

    [TestMethod]
    public async Task BuildSignalFromResponseAsync_WhenSuggestionBuy_ReturnsBuy()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new TestableOpenClawStrategy(mockService.Object);

        var response = new OpenClawResponse
        {
            Suggestion = "buy",
            Confidence = 0.75,
            Reason = "Strong momentum"
        };

        var signal = await strategy.CallBuildSignalFromResponseAsync(response);

        Assert.AreEqual(SignalType.Buy, signal.Type);
        Assert.AreEqual("Strong momentum", signal.Reason);
        Assert.AreEqual(0.75, signal.Confidence);
    }

    [TestMethod]
    public async Task BuildSignalFromResponseAsync_WhenSuggestionUnknown_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new TestableOpenClawStrategy(mockService.Object);

        var response = new OpenClawResponse
        {
            Suggestion = "dance",
            Confidence = 0.2
        };

        var signal = await strategy.CallBuildSignalFromResponseAsync(response);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("OpenClaw suggested dance", signal.Reason);
        Assert.AreEqual(0.2, signal.Confidence);
    }

    [TestMethod]
    public async Task BuildSignalFromResponseAsync_ConfidenceOutsideBounds_IsClamped()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new TestableOpenClawStrategy(mockService.Object);

        var response = new OpenClawResponse
        {
            Suggestion = "sell",
            Confidence = 1.3,
            Reason = "Too high"
        };

        var signal = await strategy.CallBuildSignalFromResponseAsync(response);

        Assert.AreEqual(SignalType.Sell, signal.Type);
        Assert.AreEqual(1.0, signal.Confidence); // from CreateSignal (Math.Clamp)
    }
}
