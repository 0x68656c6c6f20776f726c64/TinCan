using Newtonsoft.Json;

namespace TinCan.Models;

public class TradingAgentJob
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = "";

    [JsonProperty("date")]
    public string Date { get; set; } = "";

    [JsonProperty("analysts")]
    public string Analysts { get; set; } = "";

    [JsonProperty("depth")]
    public int Depth { get; set; }

    [JsonProperty("llm")]
    public string Llm { get; set; } = "";

    [JsonProperty("results_path")]
    public string ResultsPath { get; set; } = "";

    [JsonProperty("schedule_time")]
    public string ScheduleTime { get; set; } = "";

    [JsonProperty("tradingagents_path")]
    public string TradingagentsPath { get; set; } = "";

    [JsonProperty("scheduled_at")]
    public DateTime ScheduledAt { get; set; }
}
