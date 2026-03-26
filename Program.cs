using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TinCan;

public class Settings
{
    public Providers? providers { get; set; }
    public Scheduler? scheduler { get; set; }
    public string? results_dir { get; set; }
}

public class Providers
{
    public Finnhub? finnhub { get; set; }
}

public class Finnhub
{
    public string? api_key { get; set; }
    public int timeout { get; set; } = 5;
    public bool enabled { get; set; } = true;
}

public class Scheduler
{
    public int interval_minutes { get; set; } = 5;
}

public class StockLookup
{
    public Dictionary<string, StockInfo>? stocks { get; set; }
}

public class StockInfo
{
    public bool enabled { get; set; }
    public string? output { get; set; }
}

class Program
{
    private static string PROJECT_DIR = AppContext.BaseDirectory;
    private static string SETTINGS_FILE = Path.Combine(PROJECT_DIR, "settings.json");
    private static string LOOKUP_FILE = Path.Combine(PROJECT_DIR, "stock_bot", "stock_lookup.json");
    private static string RESULTS_DIR = Path.Combine(PROJECT_DIR, "stock_bot", "results");
    private static readonly HttpClient http = new();

    static Settings LoadSettings()
    {
        if (!File.Exists(SETTINGS_FILE))
        {
            Console.WriteLine($"[WARN] settings.json not found at {SETTINGS_FILE}");
            return new Settings();
        }
        var json = File.ReadAllText(SETTINGS_FILE);
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }

    static StockLookup LoadLookup()
    {
        if (!File.Exists(LOOKUP_FILE))
        {
            Console.WriteLine($"[WARN] stock_lookup.json not found at {LOOKUP_FILE}");
            return new StockLookup();
        }
        var json = File.ReadAllText(LOOKUP_FILE);
        return JsonConvert.DeserializeObject<StockLookup>(json) ?? new StockLookup();
    }

    static List<string> GetEnabledStocks(StockLookup lookup)
    {
        return lookup.stocks?
            .Where(kvp => kvp.Value.enabled)
            .Select(kvp => kvp.Key)
            .ToList() ?? new List<string>();
    }

    static string GetOutputFile(string symbol, StockLookup lookup)
    {
        if (lookup.stocks?.ContainsKey(symbol) == true && !string.IsNullOrEmpty(lookup.stocks[symbol].output))
            return lookup.stocks[symbol].output!;
        return $"{symbol.ToLower()}_stock.json";
    }

    static async Task RunFinnhub(Settings settings)
    {
        var finnhub = settings.providers?.finnhub;
        if (finnhub == null || !finnhub.enabled)
        {
            Console.WriteLine("[INFO] Finnhub provider is disabled");
            return;
        }

        var apiKey = finnhub.api_key ?? "";
        var timeout = finnhub.timeout;
        http.Timeout = TimeSpan.FromSeconds(timeout);

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your_finnhub_api_key")
        {
            Console.WriteLine("[ERROR] Finnhub API key not set in settings.json");
            return;
        }

        var lookup = LoadLookup();
        var stocks = GetEnabledStocks(lookup);

        if (stocks.Count == 0)
        {
            Console.WriteLine("[INFO] No stocks to track");
            return;
        }

        Console.WriteLine($"[INFO] Checking {stocks.Count} stock(s): {string.Join(", ", stocks)}");

        foreach (var symbol in stocks)
        {
            try
            {
                var price = await FetchPriceAsync(symbol, apiKey);
                if (price.HasValue)
                {
                    UpdateStockFile(symbol, price.Value.price, price.Value.high, price.Value.low, lookup);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {symbol}: {ex.Message}");
            }
        }
    }

    static async Task<(double price, double high, double low)?> FetchPriceAsync(string symbol, string apiKey)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";
        var response = await http.GetStringAsync(url);
        var data = JObject.Parse(response);
        var price = data["c"]?.Value<double>() ?? 0;

        if (price > 0)
        {
            return (
                price: price,
                high: data["h"]?.Value<double>() ?? price,
                low: data["l"]?.Value<double>() ?? price
            );
        }
        return null;
    }

    static void UpdateStockFile(string symbol, double price, double high, double low, StockLookup lookup)
    {
        var outputFile = GetOutputFile(symbol, lookup);
        var filepath = Path.Combine(RESULTS_DIR, outputFile);

        List<object> data = new();
        if (File.Exists(filepath))
        {
            try
            {
                var existing = File.ReadAllText(filepath);
                data = JsonConvert.DeserializeObject<List<object>>(existing) ?? new();
            }
            catch { }
        }

        var entry = new Dictionary<string, object>
        {
            ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss CT"),
            ["price"] = price,
            ["high"] = high,
            ["low"] = low
        };

        data.Add(entry);
        Directory.CreateDirectory(RESULTS_DIR);
        File.WriteAllText(filepath, JsonConvert.SerializeObject(data, Formatting.Indented));

        Console.WriteLine($"[INFO] {symbol}: ${price} -> {outputFile}");
    }

    static void RunProviders(Settings settings)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running providers...");
        RunFinnhub(settings).Wait();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" TinCan - Starting up");
        Console.WriteLine("==================================================");

        var settings = LoadSettings();
        var intervalMinutes = settings.scheduler?.interval_minutes ?? 5;

        Console.WriteLine($" Interval: {intervalMinutes} minute(s)");
        Console.WriteLine($" Settings: {SETTINGS_FILE}");
        Console.WriteLine("==================================================");

        // Run once immediately
        RunProviders(settings);

        // Main loop
        while (true)
        {
            try
            {
                Thread.Sleep(TimeSpan.FromMinutes(intervalMinutes));
                RunProviders(settings);
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("[INFO] Shutting down...");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }
    }
}
