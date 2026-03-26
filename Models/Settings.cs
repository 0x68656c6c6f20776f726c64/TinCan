using Newtonsoft.Json;

namespace TinCan.Models;

public class Settings
{
    [JsonProperty("providers")]
    public Providers? Providers { get; set; }
    
    [JsonProperty("scheduler")]
    public Scheduler? Scheduler { get; set; }
    
    [JsonProperty("results_dir")]
    public string? ResultsDir { get; set; }
}

public class Providers
{
    [JsonProperty("finnhub")]
    public FinnhubConfig? Finnhub { get; set; }
}

public class FinnhubConfig
{
    [JsonProperty("api_key")]
    public string? ApiKey { get; set; }
    
    [JsonProperty("timeout")]
    public int Timeout { get; set; } = 5;
    
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;
}

public class Scheduler
{
    [JsonProperty("interval_minutes")]
    public int IntervalMinutes { get; set; } = 5;
}
