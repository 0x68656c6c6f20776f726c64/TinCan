using Newtonsoft.Json;

namespace TinCan.Models;

public class TradingAgentStatus
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = "";

    [JsonProperty("start_time")]
    public DateTime StartTime { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = "";

    [JsonProperty("pid")]
    public int Pid { get; set; }
}
