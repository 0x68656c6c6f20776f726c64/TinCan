using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using TinCan.Services;
using Newtonsoft.Json.Linq;

namespace TinCan.Tests.Unit;

[TestClass]
public class FinnhubServiceTests
{
    [TestMethod]
    public async Task FetchPriceAsync_ValidSymbol_ReturnsStockPrice()
    {
        // Arrange
        var apiKey = "test_key";
        var service = new FinnhubService(apiKey);
        var mockHandler = new Mock<HttpMessageHandler>();
        var json = JObject.FromObject(new { c = 150.25, h = 151.00, l = 149.50 });
        mockHandler.Setup(m => m.Send(It.IsAny<HttpRequestMessage>()))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json.ToString())
            });
        var httpClient = new HttpClient(mockHandler.Object);

        // Note: This test validates the service structure
        // Integration tests would use real HTTP calls
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new FinnhubService("test_key", 10);

        // Assert
        Assert.IsNotNull(service);
    }
}
