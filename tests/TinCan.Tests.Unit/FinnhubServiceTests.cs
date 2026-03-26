using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class FinnhubServiceTests
{
    [TestMethod]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new FinnhubService("test_key", 10);

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
        Assert.IsTrue(price.High >= price.Price);
        Assert.IsTrue(price.Low <= price.Price);
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
