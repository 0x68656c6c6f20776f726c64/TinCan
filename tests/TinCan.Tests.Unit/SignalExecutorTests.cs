using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;
using TinCan.Strategies;

namespace TinCan.Tests.Unit;

[TestClass]
public class SignalExecutorTests
{
    private Mock<IBrokerService> _mockBroker = null!;
    private Mock<IMarketDataProviderService> _mockMarketData = null!;
    private Mock<IStrategy> _mockStrategy = null!;
    private SignalExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockBroker = new Mock<IBrokerService>();
        _mockMarketData = new Mock<IMarketDataProviderService>();
        _mockStrategy = new Mock<IStrategy>();
        _executor = new SignalExecutor(_mockStrategy.Object, _mockBroker.Object, _mockMarketData.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_HoldSignal_ReturnsSuccessWithNoOrder()
    {
        var signal = new Signal { Type = SignalType.Hold, Symbol = "AAPL", Quantity = 100 };

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.Order);
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<OrderSide>(), It.IsAny<OrderType>(), It.IsAny<double?>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_BuySignal_PlacesOrder()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = 100 };
        var expectedOrder = new Order { Id = "test-order-1", Symbol = "AAPL", Quantity = 100, Side = OrderSide.Buy, Status = OrderStatus.Filled };

        _mockBroker.Setup(b => b.PlaceOrderAsync("AAPL", 100, OrderSide.Buy, OrderType.Market, null))
            .ReturnsAsync(new OrderResult { Success = true, Order = expectedOrder });

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual("AAPL", result.Order.Symbol);
        Assert.AreEqual(OrderSide.Buy, result.Order.Side);
        Assert.AreEqual(100, result.Order.Quantity);
        _mockBroker.Verify(b => b.PlaceOrderAsync("AAPL", 100, OrderSide.Buy, OrderType.Market, null), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_SellSignal_PlacesOrder()
    {
        var signal = new Signal { Type = SignalType.Sell, Symbol = "AAPL", Quantity = 50 };
        var expectedOrder = new Order { Id = "test-order-2", Symbol = "AAPL", Quantity = 50, Side = OrderSide.Sell, Status = OrderStatus.Filled };

        _mockBroker.Setup(b => b.PlaceOrderAsync("AAPL", 50, OrderSide.Sell, OrderType.Market, null))
            .ReturnsAsync(new OrderResult { Success = true, Order = expectedOrder });

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderSide.Sell, result.Order.Side);
        Assert.AreEqual(50, result.Order.Quantity);
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidQuantity_ReturnsFailure()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = 0 };

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid quantity: 0", result.ErrorMessage);
        Assert.IsNull(result.Order);
    }

    [TestMethod]
    public async Task ExecuteAsync_NegativeQuantity_ReturnsFailure()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = -10 };

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid quantity: -10", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_LimitOrder_PassesLimitPrice()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = 100, OrderType = OrderType.Limit, LimitPrice = 150.00 };
        var expectedOrder = new Order { Id = "test-order-3", Symbol = "AAPL", Quantity = 100, Side = OrderSide.Buy, Type = OrderType.Limit, LimitPrice = 150.00, Status = OrderStatus.Filled };

        _mockBroker.Setup(b => b.PlaceOrderAsync("AAPL", 100, OrderSide.Buy, OrderType.Limit, 150.00))
            .ReturnsAsync(new OrderResult { Success = true, Order = expectedOrder });

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual(OrderType.Limit, result.Order.Type);
        Assert.AreEqual(150.00, result.Order.LimitPrice);
    }

    [TestMethod]
    public async Task ExecuteAsync_BrokerError_ReturnsFailure()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = 100 };

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<OrderSide>(), It.IsAny<OrderType>(), It.IsAny<double?>()))
            .ReturnsAsync(new OrderResult { Success = false, ErrorMessage = "Connection refused" });

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Connection refused", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsync_BrokerThrowsException_ReturnsFailure()
    {
        var signal = new Signal { Type = SignalType.Buy, Symbol = "AAPL", Quantity = 100 };

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<OrderSide>(), It.IsAny<OrderType>(), It.IsAny<double?>()))
            .ThrowsAsync(new Exception("Network error"));

        var result = await _executor.ExecuteAsync(signal);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Network error", result.ErrorMessage);
    }
}
