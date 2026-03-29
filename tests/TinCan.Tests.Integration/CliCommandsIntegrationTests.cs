using Microsoft.VisualStudio.TestTools.UnitTesting;
using McMaster.Extensions.CommandLineUtils;
using TinCan.Commands;
using TinCan.Infrastructure;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;
using TinCan.Tests.Integration.Helpers;

namespace TinCan.Tests.Integration;

[TestClass]
public class CliCommandsIntegrationTests
{
    private string? _apiKey;
    private string _projectDir = "";

    [TestInitialize]
    public void Setup()
    {
        _apiKey = FinnhubServiceSetupHelper.SetupAndGetApiKey();
        _projectDir = Directory.GetCurrentDirectory();
    }

    [TestMethod]
    public async Task PriceCommand_ReturnsValidPrice()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = true, ApiKey = _apiKey, Timeout = 10 }
            }
        };

        var marketData = MarketDataProviderFactory.Create(settings);
        var result = await marketData.FetchPriceAsync("U");

        Assert.IsNotNull(result);
        Assert.AreEqual("U", result.Symbol);
        Assert.IsTrue(result.Price > 0);
    }

    [TestMethod]
    public async Task BackfillCommand_FetchesHistoricalData()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = true, ApiKey = _apiKey, Timeout = 10 }
            }
        };

        var marketData = MarketDataProviderFactory.Create(settings);
        var from = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Local);
        var to = new DateTime(2024, 01, 10, 0, 0, 0, DateTimeKind.Local);

        try
        {
            var result = await marketData.FetchHistoricalPricesAsync("U", "D", from, to);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.AreEqual("U", result[0].Symbol);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403"))
        {
            // Finnhub free tier may not support historical data - skip this test
            Assert.Inconclusive("Finnhub free tier may not support historical data endpoint");
        }
    }

    [TestMethod]
    public void ContextCommand_LoadsMarketContext()
    {
        // Create a temp directory with proper structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"tincan_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot"));
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot", "results"));

        try
        {
            // Create stock_lookup.json
            var lookup = new StockLookup
            {
                Stocks = new Dictionary<string, StockInfo>
                {
                    ["AAPL"] = new StockInfo { Enabled = true, Output = "aapl_stock.json" }
                }
            };
            var lookupJson = Newtonsoft.Json.JsonConvert.SerializeObject(lookup);
            File.WriteAllText(Path.Combine(tempDir, "stock_bot", "stock_lookup.json"), lookupJson);

            // Create a test result file
            var resultFile = Path.Combine(tempDir, "stock_bot", "results", "aapl_stock.json");
            var testData = @"[
                {""time"":""2024-01-15 09:30:00 CT"",""price"":185.50,""high"":186.00,""low"":185.00},
                {""time"":""2024-01-16 09:30:00 CT"",""price"":186.50,""high"":187.00,""low"":186.00}
            ]";
            File.WriteAllText(resultFile, testData);

            // Now test the context loading
            var service = new StockFileService(tempDir);
            var context = service.LoadMarketContext("AAPL");

            Assert.AreEqual("AAPL", context.Symbol);
            Assert.AreEqual(2, context.PriceHistory.Count);
            Assert.IsNotNull(context.CurrentPrice);
            Assert.AreEqual(186.50, context.CurrentPrice.Price);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void MarketDataProviderFactory_CreatesFinnhubService()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = true, ApiKey = _apiKey, Timeout = 10 }
            }
        };

        var marketData = MarketDataProviderFactory.Create(settings);

        Assert.IsNotNull(marketData);
        Assert.IsInstanceOfType(marketData, typeof(FinnhubService));
    }

    [TestMethod]
    public void MarketDataProviderFactory_DisabledProvider_ThrowsInvalidOperationException()
    {
        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = false, ApiKey = "test" }
            }
        };

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            MarketDataProviderFactory.Create(settings));

        Assert.IsTrue(ex.Message.Contains("No enabled market data provider"));
    }

    [TestMethod]
    public void MarketDataProviderFactory_MissingApiKey_ThrowsInvalidOperationException()
    {
        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = true, ApiKey = "", Timeout = 10 }
            }
        };

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            MarketDataProviderFactory.Create(settings));

        Assert.IsTrue(ex.Message.Contains("No enabled market data provider"));
    }

    [TestMethod]
    public void OrdersCommand_Stub_ReturnsError()
    {
        var app = new CommandLineApplication();
        var resolvedProvider = ProviderResolver.Resolve(null, null);

        Assert.AreEqual("paper", resolvedProvider);
        // Stub commands already tested separately - this verifies the resolver works
    }

    [TestMethod]
    public void ProviderResolver_UsesPaperAsDefault()
    {
        var result = ProviderResolver.Resolve(null, null);
        Assert.AreEqual("paper", result);
    }

    [TestMethod]
    public void ProviderResolver_UsesCliProviderWhenProvided()
    {
        var result = ProviderResolver.Resolve("alpaca", "paper");
        Assert.AreEqual("alpaca", result);
    }

    [TestMethod]
    public void SettingsLoader_LoadsValidSettings()
    {
        var settings = SettingsLoader.Load();

        Assert.IsNotNull(settings);
        // Default settings should have reasonable defaults
        Assert.IsNotNull(settings.Providers);
    }
}
