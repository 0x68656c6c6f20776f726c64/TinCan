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
    [JsonProperty("paper")]
    public PaperConfig? Paper { get; set; }
    
    [JsonProperty("alpaca")]
    public AlpacaConfig? Alpaca { get; set; }
}

public class PaperConfig
{
    [JsonProperty("initialCash")]
    public double InitialCash { get; set; } = 10000.00;
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
