namespace TinCan.Models;

public class Settings
{
    public Providers? Providers { get; set; }
    public Scheduler? Scheduler { get; set; }
    public string? ResultsDir { get; set; }
}

public class Providers
{
    public FinnhubConfig? Finnhub { get; set; }
}

public class FinnhubConfig
{
    public string? ApiKey { get; set; }
    public int Timeout { get; set; } = 5;
    public bool Enabled { get; set; } = true;
}

public class Scheduler
{
    public int IntervalMinutes { get; set; } = 5;
}
