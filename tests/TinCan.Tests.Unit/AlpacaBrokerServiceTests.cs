using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;

namespace TinCan.Tests.Unit;

[TestClass]
public class AlpacaBrokerServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpHandler = null!;
    private AlpacaBrokerService _brokerService = null!;

    private const string TestApiKey = "test_api_key";
    private const string TestSecretKey = "test_secret_key";
    private const string TestBaseUrl = "https://paper-api.alpaca.markets";

    private void SetupHttpClient(HttpResponseMessage response)
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri(TestBaseUrl)
        };

        // Use reflection to set the HttpClient
        var serviceType = typeof(AlpacaBrokerService);
        var httpClientField = serviceType.GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _brokerService = new AlpacaBrokerService(TestApiKey, TestSecretKey, TestBaseUrl);
        httpClientField?.SetValue(_brokerService, httpClient);
    }

    [TestMethod]
    public async Task GetBalanceAsync_HappyPath_ReturnsAccountBalance()
    {
        // Arrange
        var json = @"{
            ""cash"": ""10000.50"",
            ""portfolio_value"": ""15000.75""
        }";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.GetBalanceAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(10000.50, result.Cash);
        Assert.AreEqual(15000.75, result.Equity);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_HappyPath_ReturnsOrderResult()
    {
        // Arrange
        var json = @"{
            ""id"": ""order-123"",
            ""symbol"": ""AAPL"",
            ""qty"": 10,
            ""side"": ""buy"",
            ""type"": ""market"",
            ""status"": ""new"",
            ""created_at"": ""2024-01-15T10:30:00Z""
        }";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.PlaceOrderAsync("AAPL", 10, OrderSide.Buy, OrderType.Market);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNotNull(result.Order);
        Assert.AreEqual("order-123", result.Order.Id);
        Assert.AreEqual("AAPL", result.Order.Symbol);
        Assert.AreEqual(10, result.Order.Quantity);
    }

    [TestMethod]
    public async Task GetOpenOrdersAsync_HappyPath_ReturnsOrderList()
    {
        // Arrange
        var json = @"[
            {
                ""id"": ""order-1"",
                ""symbol"": ""AAPL"",
                ""qty"": 10,
                ""side"": ""buy"",
                ""type"": ""market"",
                ""status"": ""new"",
                ""created_at"": ""2024-01-15T10:30:00Z""
            },
            {
                ""id"": ""order-2"",
                ""symbol"": ""MSFT"",
                ""qty"": 5,
                ""side"": ""sell"",
                ""type"": ""limit"",
                ""status"": ""new"",
                ""created_at"": ""2024-01-15T11:00:00Z"",
                ""limit_price"": ""350.00""
            }
        ]";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.GetOpenOrdersAsync("");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("order-1", result[0].Id);
        Assert.AreEqual("AAPL", result[0].Symbol);
        Assert.AreEqual("order-2", result[1].Id);
        Assert.AreEqual("MSFT", result[1].Symbol);
    }

    [TestMethod]
    public async Task GetOrderHistoryAsync_HappyPath_ReturnsClosedOrders()
    {
        // Arrange
        var json = @"[
            {
                ""id"": ""order-1"",
                ""symbol"": ""AAPL"",
                ""qty"": 10,
                ""side"": ""buy"",
                ""type"": ""market"",
                ""status"": ""filled"",
                ""created_at"": ""2024-01-15T10:30:00Z"",
                ""filled_at"": ""2024-01-15T10:30:05Z"",
                ""filled_avg_price"": ""180.00""
            }
        ]";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.GetOrderHistoryAsync("AAPL", 50);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("order-1", result[0].Id);
        Assert.AreEqual(OrderStatus.Filled, result[0].Status);
        Assert.AreEqual(180.00, result[0].FillPrice);
    }

    [TestMethod]
    public async Task CancelOrderAsync_HappyPath_ReturnsTrue()
    {
        // Arrange
        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        });

        // Act
        var result = await _brokerService.CancelOrderAsync("order-123");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetPositionsAsync_HappyPath_ReturnsPositionList()
    {
        // Arrange
        var json = @"[
            {
                ""symbol"": ""AAPL"",
                ""qty"": ""100"",
                ""avg_entry_price"": ""150.00"",
                ""market_value"": ""16000.00"",
                ""unrealized_pl"": ""1000.00""
            },
            {
                ""symbol"": ""MSFT"",
                ""qty"": ""50"",
                ""avg_entry_price"": ""300.00"",
                ""market_value"": ""17500.00"",
                ""unrealized_pl"": ""2500.00""
            }
        ]";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.GetPositionsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        
        Assert.AreEqual("AAPL", result[0].Symbol);
        Assert.AreEqual(100, result[0].Quantity);
        Assert.AreEqual(150.00, result[0].AvgCost);
        Assert.AreEqual(16000.00, result[0].MarketValue);
        Assert.AreEqual(1000.00, result[0].UnrealizedPnL);
        
        Assert.AreEqual("MSFT", result[1].Symbol);
        Assert.AreEqual(50, result[1].Quantity);
    }

    [TestMethod]
    public async Task GetPositionsAsync_SingleSymbol_ReturnsPosition()
    {
        // Arrange
        var json = @"{
            ""symbol"": ""AAPL"",
            ""qty"": ""100"",
            ""avg_entry_price"": ""150.00"",
            ""market_value"": ""16000.00"",
            ""unrealized_pl"": ""1000.00""
        }";

        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        // Act
        var result = await _brokerService.GetPositionsAsync("AAPL");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("AAPL", result[0].Symbol);
        Assert.AreEqual(100, result[0].Quantity);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_FailedOrder_ReturnsFailureResult()
    {
        // Arrange
        SetupHttpClient(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(@"{ ""message"": ""Insufficient buying power"" }")
        });

        // Act
        var result = await _brokerService.PlaceOrderAsync("AAPL", 1000, OrderSide.Buy, OrderType.Market);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNull(result.Order);
    }
}
