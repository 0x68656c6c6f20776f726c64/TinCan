using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TinCan.Commands;
using TinCan.Infrastructure;
using TinCan.Models;

namespace TinCan.Tests.Unit;

[TestClass]
public class TradingagentCommandTests
{
    private string _testSettingsPath = null!;
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"TinCan_TradingagentTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testSettingsPath = Path.Combine(_testDir, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public void Settings_TradingagentsConfig_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""providers"": {
                ""finnhub"": {
                    ""api_key"": ""test_key"",
                    ""timeout"": 5,
                    ""enabled"": true
                }
            },
            ""scheduler"": {
                ""interval_minutes"": 5
            },
            ""tradingagents"": {
                ""path"": ""/path/to/TradingAgents"",
                ""results_path"": ""/path/to/TradingAgents/eval_results"",
                ""default_analysts"": [""market"", ""social""],
                ""default_depth"": 3,
                ""default_llm"": ""minimax""
            }
        }";

        // Act
        var settings = JsonConvert.DeserializeObject<Settings>(json);

        // Assert
        Assert.IsNotNull(settings);
        Assert.IsNotNull(settings.Tradingagents);
        Assert.AreEqual("/path/to/TradingAgents", settings.Tradingagents.Path);
        Assert.AreEqual("/path/to/TradingAgents/eval_results", settings.Tradingagents.ResultsPath);
        Assert.AreEqual(3, settings.Tradingagents.DefaultDepth);
        Assert.AreEqual("minimax", settings.Tradingagents.DefaultLlm);
        Assert.IsNotNull(settings.Tradingagents.DefaultAnalysts);
        Assert.AreEqual(2, settings.Tradingagents.DefaultAnalysts.Count);
        Assert.AreEqual("market", settings.Tradingagents.DefaultAnalysts[0]);
    }

    [TestMethod]
    public void Settings_TradingagentsConfig_Defaults_AreCorrect()
    {
        // Arrange
        var json = @"{
            ""tradingagents"": {}
        }";

        // Act
        var settings = JsonConvert.DeserializeObject<Settings>(json);

        // Assert
        Assert.IsNotNull(settings);
        Assert.IsNotNull(settings.Tradingagents);
        Assert.AreEqual(2, settings.Tradingagents.DefaultDepth);
        Assert.AreEqual("minimax", settings.Tradingagents.DefaultLlm);
    }
}
