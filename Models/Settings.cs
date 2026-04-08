using Newtonsoft.Json;

namespace TinCan.Models;

public class Settings
{
    [JsonProperty("providers")]
    public Providers? Providers { get; set; }
    
    [JsonProperty("scheduler")]
    public Scheduler? Scheduler { get; set; }
    
    [JsonProperty("broker")]
    public BrokerConfig? Broker { get; set; }
    
    [JsonProperty("results_dir")]
    public string? ResultsDir { get; set; }

    [JsonProperty("tradingagents")]
    public TradingagentsConfig? Tradingagents { get; set; }
}

public class Providers
{
    [JsonProperty("finnhub")]
    public FinnhubConfig? Finnhub { get; set; }
    
    [JsonProperty("broker")]
    public string? Broker { get; set; }
}

public class BrokerConfig
{
    [JsonProperty("alpaca")]
    public AlpacaConfig? Alpaca { get; set; }
}

public class AlpacaConfig
{
    [JsonProperty("apiKey")]
    public string? ApiKey { get; set; }
    
    [JsonProperty("secretKey")]
    public string? SecretKey { get; set; }
    
    [JsonProperty("baseUrl")]
    public string? BaseUrl { get; set; }
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

    [JsonProperty("tradingagent_time")]
    public string? TradingagentTime { get; set; }

    [JsonProperty("historical")]
    public HistoricalSchedulerConfig? Historical { get; set; }
}

public class HistoricalSchedulerConfig
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("resolution")]
    public string Resolution { get; set; } = "D";

    [JsonProperty("lookback_days")]
    public int LookbackDays { get; set; } = 30;
}

public class TradingagentsConfig
{
    [JsonProperty("path")]
    public string? Path { get; set; }

    [JsonProperty("results_path")]
    public string? ResultsPath { get; set; }

    [JsonProperty("default_analysts")]
    public List<string>? DefaultAnalysts { get; set; }

    [JsonProperty("default_depth")]
    public int DefaultDepth { get; set; } = 2;

    [JsonProperty("default_llm")]
    public string? DefaultLlm { get; set; } = "minimax";
}
