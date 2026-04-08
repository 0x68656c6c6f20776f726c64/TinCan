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
    public void TradingAgentJob_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var job = new TradingAgentJob
        {
            Symbol = "AAPL",
            Date = "2026-04-08",
            Analysts = "market,news",
            Depth = 3,
            Llm = "minimax",
            ResultsPath = "/path/to/results",
            ScheduleTime = "09:30",
            TradingagentsPath = "/path/to/TradingAgents",
            ScheduledAt = new DateTime(2026, 4, 8, 8, 0, 0)
        };

        // Act
        var json = JsonConvert.SerializeObject(job);
        var deserialized = JsonConvert.DeserializeObject<TradingAgentJob>(json);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.AreEqual("AAPL", deserialized.Symbol);
        Assert.AreEqual("2026-04-08", deserialized.Date);
        Assert.AreEqual("market,news", deserialized.Analysts);
        Assert.AreEqual(3, deserialized.Depth);
        Assert.AreEqual("minimax", deserialized.Llm);
        Assert.AreEqual("/path/to/results", deserialized.ResultsPath);
        Assert.AreEqual("09:30", deserialized.ScheduleTime);
        Assert.AreEqual("/path/to/TradingAgents", deserialized.TradingagentsPath);
        Assert.AreEqual(new DateTime(2026, 4, 8, 8, 0, 0), deserialized.ScheduledAt);
    }

    [TestMethod]
    public void TradingAgentStatus_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var status = new TradingAgentStatus
        {
            Symbol = "NVDA",
            StartTime = new DateTime(2026, 4, 8, 9, 0, 0),
            Status = "Running",
            Pid = 12345
        };

        // Act
        var json = JsonConvert.SerializeObject(status);
        var deserialized = JsonConvert.DeserializeObject<TradingAgentStatus>(json);

        // Assert
        Assert.IsNotNull(deserialized);
        Assert.AreEqual("NVDA", deserialized.Symbol);
        Assert.AreEqual("Running", deserialized.Status);
        Assert.AreEqual(12345, deserialized.Pid);
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
                ""interval_minutes"": 5,
                ""tradingagent_time"": ""09:30""
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
        Assert.AreEqual("09:30", settings.Scheduler?.TradingagentTime);
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
