using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinCan.Models;

namespace TinCan.Services;

public class FinnhubService
{
    private readonly string _apiKey;
    private readonly int _timeout;
    private readonly HttpClient _http;
    private readonly string _lookupFile;
    private readonly string _resultsDir;

    public FinnhubService(FinnhubConfig config, string projectDir)
    {
        _apiKey = config.ApiKey ?? "";
        _timeout = config.Timeout;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(_timeout) };
        _lookupFile = Path.Combine(projectDir, "stock_bot", "stock_lookup.json");
        _resultsDir = Path.Combine(projectDir, "stock_bot", "results");
    }

    public bool IsEnabled => !string.IsNullOrEmpty(_apiKey) && _apiKey != "your_finnhub_api_key";

    private StockLookup LoadLookup()
    {
        if (!File.Exists(_lookupFile))
            return new StockLookup();
        var json = File.ReadAllText(_lookupFile);
        return JsonConvert.DeserializeObject<StockLookup>(json) ?? new StockLookup();
    }

    private List<string> GetEnabledStocks(StockLookup lookup)
    {
        return lookup.Stocks?
            .Where(kvp => kvp.Value.Enabled)
            .Select(kvp => kvp.Key)
            .ToList() ?? new List<string>();
    }

    private string GetOutputFile(string symbol, StockLookup lookup)
    {
        if (lookup.Stocks?.ContainsKey(symbol) == true && !string.IsNullOrEmpty(lookup.Stocks[symbol].Output))
            return lookup.Stocks[symbol].Output!;
        return $"{symbol.ToLower()}_stock.json";
    }

    private async Task<(double price, double high, double low)?> FetchPriceAsync(string symbol)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_apiKey}";
        var response = await _http.GetStringAsync(url);
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

    private void UpdateStockFile(string symbol, double price, double high, double low, StockLookup lookup)
    {
        var outputFile = GetOutputFile(symbol, lookup);
        var filepath = Path.Combine(_resultsDir, outputFile);

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
        Directory.CreateDirectory(_resultsDir);
        File.WriteAllText(filepath, JsonConvert.SerializeObject(data, Formatting.Indented));

        Console.WriteLine($"[INFO] {symbol}: ${price} -> {outputFile}");
    }

    public async Task RunAsync()
    {
        if (!IsEnabled)
        {
            Console.WriteLine("[INFO] Finnhub provider is disabled or API key not set");
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
                var price = await FetchPriceAsync(symbol);
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
}
