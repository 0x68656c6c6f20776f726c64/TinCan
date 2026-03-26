using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class FinnhubServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpHandler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
    }

    [TestMethod]
    public async Task FetchPriceAsync_ValidSymbol_ReturnsStockPrice()
    {
        // Arrange
        var json = @"{
            ""c"": 150.25,
            ""h"": 151.00,
            ""l"": 149.50,
            ""o"": 150.00,
            ""pc"": 149.00,
            ""t"": 1234567890
        }";

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("U")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var service = new FinnhubService("test_key", httpClient);

        // Act
        var result = await service.FetchPriceAsync("U");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("U", result.Symbol);
        Assert.AreEqual(150.25, result.Price);
        Assert.AreEqual(151.00, result.High);
        Assert.AreEqual(149.50, result.Low);
    }

    [TestMethod]
    public async Task FetchPriceAsync_InvalidSymbol_ReturnsNull()
    {
        // Arrange
        var json = @"{""c"":0,""h"":0,""l"":0}";

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var service = new FinnhubService("test_key", httpClient);

        // Act
        var result = await service.FetchPriceAsync("INVALID");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Constructor_WithHttpClient_InitializesCorrectly()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new FinnhubService("test_key", httpClient);

        // Assert
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void StockPrice_Model_HoldsCorrectValues()
    {
        // Arrange & Act
        var price = new StockPrice
        {
            Symbol = "AAPL",
            Price = 150.25,
            High = 151.00,
            Low = 149.50,
            Timestamp = DateTime.Now
        };

        // Assert
        Assert.AreEqual("AAPL", price.Symbol);
        Assert.AreEqual(150.25, price.Price);
        Assert.AreEqual(151.00, price.High);
        Assert.AreEqual(149.50, price.Low);
    }

    [TestMethod]
    public void Settings_Model_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""providers"": {
                ""finnhub"": {
                    ""api_key"": ""test_key"",
                    ""timeout"": 10,
                    ""enabled"": true
                }
            },
            ""scheduler"": {
                ""interval_minutes"": 5
            }
        }";

        // Act
        var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);

        // Assert
        Assert.IsNotNull(settings);
        Assert.AreEqual("test_key", settings.Providers?.Finnhub?.ApiKey);
        Assert.AreEqual(10, settings.Providers?.Finnhub?.Timeout);
        Assert.IsTrue(settings.Providers?.Finnhub?.Enabled);
        Assert.AreEqual(5, settings.Scheduler?.IntervalMinutes);
    }
}
