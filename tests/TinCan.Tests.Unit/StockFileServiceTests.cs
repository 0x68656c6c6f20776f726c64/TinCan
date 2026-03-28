using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinCan.Services;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class StockFileServiceTests
{
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "TinCan_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_testDir, "stock_bot", "results"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [TestMethod]
    public void Constructor_SetsCorrectPaths()
    {
        var service = new StockFileService(_testDir);
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void LoadLookup_ReturnsEmptyLookup_WhenFileNotFound()
    {
        var service = new StockFileService(_testDir);
        var result = service.LoadLookup();

        Assert.IsNotNull(result);
        Assert.IsNull(result.Stocks);
    }

    [TestMethod]
    public void LoadLookup_WithValidFile_ReturnsDeserializedLookup()
    {
        var lookupJson = @"{
            ""stocks"": {
                ""AAPL"": {
                    ""enabled"": true,
                    ""output"": ""aapl.json""
                }
            }
        }";
        var stockBotDir = Path.Combine(_testDir, "stock_bot");
        Directory.CreateDirectory(stockBotDir);
        File.WriteAllText(Path.Combine(stockBotDir, "stock_lookup.json"), lookupJson);

        var service = new StockFileService(_testDir);
        var result = service.LoadLookup();

        Assert.IsNotNull(result.Stocks);
        Assert.IsTrue(result.Stocks.ContainsKey("AAPL"));
        Assert.AreEqual("aapl.json", result.Stocks["AAPL"].Output);
        Assert.IsTrue(result.Stocks["AAPL"].Enabled);
    }

    [TestMethod]
    public void GetEnabledStocks_AllEnabled_ReturnsAllSymbols()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true },
                ["U"] = new StockInfo { Enabled = true },
                ["GOOG"] = new StockInfo { Enabled = false }
            }
        };

        var result = service.GetEnabledStocks(lookup);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains("AAPL"));
        Assert.IsTrue(result.Contains("U"));
        Assert.IsFalse(result.Contains("GOOG"));
    }

    [TestMethod]
    public void GetEnabledStocks_NullStocks_ReturnsEmptyList()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup { Stocks = null };

        var result = service.GetEnabledStocks(lookup);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetOutputFile_CustomOutput_ReturnsCustomFile()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true, Output = "my_aapl.json" }
            }
        };

        var result = service.GetOutputFile("AAPL", lookup);

        Assert.AreEqual("my_aapl.json", result);
    }

    [TestMethod]
    public void GetOutputFile_NoCustomOutput_ReturnsDefaultFile()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true }
            }
        };

        var result = service.GetOutputFile("AAPL", lookup);

        Assert.AreEqual("aapl_stock.json", result);
    }

    [TestMethod]
    public void GetOutputFile_SymbolNotInLookup_ReturnsDefaultFile()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true }
            }
        };

        var result = service.GetOutputFile("UNKNOWN", lookup);

        Assert.AreEqual("unknown_stock.json", result);
    }

    [TestMethod]
    public void UpdateStockFile_CreatesNewFile()
    {
        var service = new StockFileService(_testDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true, Output = "aapl_test.json" }
            }
        };

        service.UpdateStockFile("AAPL", 150.25, 151.00, 149.50, lookup);

        var filePath = Path.Combine(_testDir, "stock_bot", "results", "aapl_test.json");
        Assert.IsTrue(File.Exists(filePath));

        var content = File.ReadAllText(filePath);
        Assert.IsTrue(content.Contains("150.25"));
    }

    [TestMethod]
    public void UpdateStockFile_AppendsToExistingFile()
    {
        var service = new StockFileService(_testDir);
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        var existingFile = Path.Combine(resultsDir, "unity_stock.json");
        File.WriteAllText(existingFile, @"[{""time"":""2024-01-01 10:00:00 CT"",""price"":17.0,""high"":17.5,""low"":16.5}]");

        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["U"] = new StockInfo { Enabled = true, Output = "unity_stock.json" }
            }
        };

        service.UpdateStockFile("U", 17.50, 18.00, 17.25, lookup);

        var content = File.ReadAllText(existingFile);
        Assert.IsTrue(content.Contains("17.0"));
        Assert.IsTrue(content.Contains("17.5"));
    }

    [TestMethod]
    public void LoadMarketContext_MissingFile_ReturnsEmptyContext()
    {
        var service = new StockFileService(_testDir);

        var result = service.LoadMarketContext("AAPL");

        Assert.AreEqual("AAPL", result.Symbol);
        Assert.IsNull(result.CurrentPrice);
        Assert.AreEqual(0, result.PriceHistory.Count);
    }

    [TestMethod]
    public void LoadMarketContext_EmptyFile_ReturnsEmptyContext()
    {
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        File.WriteAllText(Path.Combine(resultsDir, "aapl_stock.json"), "[]");

        var service = new StockFileService(_testDir);

        var result = service.LoadMarketContext("AAPL");

        Assert.AreEqual("AAPL", result.Symbol);
        Assert.IsNull(result.CurrentPrice);
        Assert.AreEqual(0, result.PriceHistory.Count);
    }

    [TestMethod]
    public void LoadMarketContext_SingleEntry_CurrentPriceEqualsHistoryEntry()
    {
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true, Output = "aapl_stock.json" }
            }
        };
        File.WriteAllText(Path.Combine(resultsDir, "aapl_stock.json"),
            @"[{""time"":""2024-01-15 09:30:00 CT"",""price"":185.50,""high"":186.00,""low"":185.00}]");

        var service = new StockFileService(_testDir);

        var result = service.LoadMarketContext("AAPL");

        Assert.AreEqual(1, result.PriceHistory.Count);
        Assert.IsNotNull(result.CurrentPrice);
        Assert.AreEqual(185.50, result.CurrentPrice.Price);
        Assert.AreEqual(186.00, result.CurrentPrice.High);
        Assert.AreEqual(185.00, result.CurrentPrice.Low);
    }

    [TestMethod]
    public void LoadMarketContext_MultipleEntries_SortedChronologically_CurrentPriceIsLast()
    {
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["U"] = new StockInfo { Enabled = true, Output = "unity_stock.json" }
            }
        };
        File.WriteAllText(Path.Combine(resultsDir, "unity_stock.json"),
            @"[{""time"":""2024-01-01 09:30:00 CT"",""price"":17.0,""high"":17.5,""low"":16.5}," +
            @"{""time"":""2024-01-02 09:30:00 CT"",""price"":18.0,""high"":18.5,""low"":17.5}," +
            @"{""time"":""2024-01-03 09:30:00 CT"",""price"":19.0,""high"":19.5,""low"":18.5}]");

        var service = new StockFileService(_testDir);
        var result = service.LoadMarketContext("U", lookup);

        Assert.AreEqual(3, result.PriceHistory.Count);
        Assert.AreEqual("U", result.Symbol);
        Assert.IsNotNull(result.CurrentPrice);
        Assert.AreEqual(19.0, result.CurrentPrice.Price);
        Assert.AreEqual(3, result.PriceHistory.Count);
        Assert.AreEqual(17.0, result.PriceHistory[0].Price);
        Assert.AreEqual(19.0, result.PriceHistory[2].Price);
    }

    [TestMethod]
    public void LoadMarketContext_UnknownSymbol_ReturnsEmptyContext()
    {
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        // Create a file for a different symbol
        File.WriteAllText(Path.Combine(resultsDir, "aapl_stock.json"),
            @"[{""time"":""2024-01-15 09:30:00 CT"",""price"":185.50,""high"":186.00,""low"":185.00}]");

        var service = new StockFileService(_testDir);

        var result = service.LoadMarketContext("UNKNOWN");

        Assert.AreEqual("UNKNOWN", result.Symbol);
        Assert.IsNull(result.CurrentPrice);
        Assert.AreEqual(0, result.PriceHistory.Count);
    }

    [TestMethod]
    public void LoadMarketContext_UTCFormat_ParsesCorrectly()
    {
        var resultsDir = Path.Combine(_testDir, "stock_bot", "results");
        Directory.CreateDirectory(resultsDir);
        var lookup = new StockLookup
        {
            Stocks = new Dictionary<string, StockInfo>
            {
                ["AAPL"] = new StockInfo { Enabled = true, Output = "aapl_stock.json" }
            }
        };
        File.WriteAllText(Path.Combine(resultsDir, "aapl_stock.json"),
            @"[{""time"":""2024-01-15 14:30:00 UTC"",""price"":185.50,""high"":186.00,""low"":185.00}]");

        var service = new StockFileService(_testDir);

        var result = service.LoadMarketContext("AAPL", lookup);

        Assert.AreEqual(1, result.PriceHistory.Count);
        Assert.IsNotNull(result.CurrentPrice);
        Assert.AreEqual(185.50, result.CurrentPrice.Price);
    }
}
