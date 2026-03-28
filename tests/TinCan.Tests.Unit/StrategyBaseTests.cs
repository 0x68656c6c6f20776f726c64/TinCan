using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Models;
using TinCan.Strategies;

namespace TinCan.Tests.Unit;

[TestClass]
public class StrategyBaseTests
{
    private class MockStrategy : StrategyBase
    {
        public override string Name => "MockStrategy";
        public Signal? ForcedSignal { get; set; }

        public override Signal Generate(MarketContext context)
        {
            return ForcedSignal ?? CreateSignal(SignalType.Hold, "No signal configured", 0.0);
        }
    }

    [TestMethod]
    public void IStrategy_Name_ReturnsCorrectName()
    {
        var strategy = new MockStrategy();
        Assert.AreEqual("MockStrategy", strategy.Name);
    }

    [TestMethod]
    public void Generate_WithValidContext_ReturnsSignal()
    {
        var strategy = new MockStrategy
        {
            ForcedSignal = new Signal
            {
                Type = SignalType.Buy,
                Reason = "Test buy",
                Confidence = 0.8
            }
        };

        var context = new MarketContext
        {
            Symbol = "AAPL",
            CurrentPrice = new StockPrice { Symbol = "AAPL", Price = 150.0 }
        };

        var signal = strategy.Generate(context);

        Assert.AreEqual(SignalType.Buy, signal.Type);
        Assert.AreEqual("Test buy", signal.Reason);
        Assert.AreEqual(0.8, signal.Confidence);
    }

    [TestMethod]
    public void CreateSignal_ClampsConfidenceToValidRange()
    {
        var strategy = new MockStrategy();

        var signal = strategy.CreateSignal(SignalType.Sell, "Test", 1.5);
        Assert.AreEqual(1.0, signal.Confidence);

        signal = strategy.CreateSignal(SignalType.Buy, "Test", -0.5);
        Assert.AreEqual(0.0, signal.Confidence);
    }

    [TestMethod]
    public void Signal_TypeEnum_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)SignalType.Buy);
        Assert.AreEqual(1, (int)SignalType.Sell);
        Assert.AreEqual(2, (int)SignalType.Hold);
    }
}
