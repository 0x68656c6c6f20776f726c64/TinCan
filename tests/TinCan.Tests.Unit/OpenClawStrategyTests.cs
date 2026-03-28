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
    public void Generate_WithNullCurrentPrice_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawStrategy(mockService.Object);

        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = null
        };

        var signal = strategy.Generate(context);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No current price available", signal.Reason);
    }
}

[TestClass]
public class OpenClawSimpleStrategyTests
{
    [TestMethod]
    public void Name_ReturnsCorrectName()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawSimpleStrategy(mockService.Object);
        Assert.AreEqual("OpenClawSimpleStrategy", strategy.Name);
    }

    [TestMethod]
    public void InheritsFromOpenClawStrategy()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var simpleStrategy = new OpenClawSimpleStrategy(mockService.Object);

        Assert.IsInstanceOfType(simpleStrategy, typeof(OpenClawStrategy));
    }

    [TestMethod]
    public void Generate_WithNullCurrentPrice_ReturnsHold()
    {
        var mockService = new Mock<OpenClawService>("http://localhost", "token");
        var strategy = new OpenClawSimpleStrategy(mockService.Object);

        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = null
        };

        var signal = strategy.Generate(context);

        Assert.AreEqual(SignalType.Hold, signal.Type);
        Assert.AreEqual("No current price available", signal.Reason);
    }
}
