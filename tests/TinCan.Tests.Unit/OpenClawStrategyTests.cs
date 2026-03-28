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
    public async Task Generate_WithNullCurrentPrice_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawStrategy(mockService.Object);

        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = null
        };

        var signal = await strategy.Generate(context);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No current price available", signal.Reason);
    }
}
