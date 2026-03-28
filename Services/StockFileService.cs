using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using TinCan.Models;

namespace TinCan.Services;

public class StockFileService
{
    private readonly string _lookupFile;
    private readonly string _resultsDir;

    public StockFileService(string projectDir)
    {
        _lookupFile = Path.Combine(projectDir, "stock_bot", "stock_lookup.json");
        _resultsDir = Path.Combine(projectDir, "stock_bot", "results");
    }

    public StockLookup LoadLookup()
    {
        if (!File.Exists(_lookupFile))
            return new StockLookup();
        var json = File.ReadAllText(_lookupFile);
        return JsonConvert.DeserializeObject<StockLookup>(json) ?? new StockLookup();
    }

    public List<string> GetEnabledStocks(StockLookup lookup)
    {
        return lookup.Stocks?
            .Where(kvp => kvp.Value.Enabled)
            .Select(kvp => kvp.Key)
            .ToList() ?? new List<string>();
    }

    public string GetOutputFile(string symbol, StockLookup lookup)
    {
        if (lookup.Stocks?.ContainsKey(symbol) == true && !string.IsNullOrEmpty(lookup.Stocks[symbol].Output))
            return lookup.Stocks[symbol].Output!;
        return $"{symbol.ToLower()}_stock.json";
    }

    public void UpdateStockFile(string symbol, double price, double high, double low, StockLookup lookup)
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

    public void ReplaceStockFileWithHistory(string symbol, IEnumerable<StockPrice> history, StockLookup lookup)
    {
        var outputFile = GetOutputFile(symbol, lookup);
        var filepath = Path.Combine(_resultsDir, outputFile);

        var data = history
            .OrderBy(price => price.Timestamp)
            .Select(price => new Dictionary<string, object>
            {
                ["time"] = price.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["price"] = price.Price,
                ["high"] = price.High,
                ["low"] = price.Low
            })
            .Cast<object>()
            .ToList();

        Directory.CreateDirectory(_resultsDir);
        File.WriteAllText(filepath, JsonConvert.SerializeObject(data, Formatting.Indented));

        Console.WriteLine($"[INFO] {symbol}: wrote {data.Count} historical data point(s) -> {outputFile}");
    }

    public MarketContext LoadMarketContext(string symbol, StockLookup? lookup = null)
    {
        lookup ??= LoadLookup();
        var outputFile = GetOutputFile(symbol, lookup);
        var filepath = Path.Combine(_resultsDir, outputFile);

        var context = new MarketContext { Symbol = symbol };

        if (!File.Exists(filepath))
            return context;

        string json;
        try
        {
            json = File.ReadAllText(filepath);
        }
        catch
        {
            return context;
        }

        List<Dictionary<string, object>> entries;
        try
        {
            entries = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json) ?? [];
        }
        catch
        {
            return context;
        }

        var ctFormat = "yyyy-MM-dd HH:mm:ss 'CT'";
        var utcFormat = "yyyy-MM-dd HH:mm:ss 'UTC'";
        var formats = new[] { ctFormat, utcFormat };

        foreach (var entry in entries)
        {
            if (!entry.TryGetValue("time", out var timeObj) ||
                !entry.TryGetValue("price", out var priceObj) ||
                !entry.TryGetValue("high", out var highObj) ||
                !entry.TryGetValue("low", out var lowObj))
                continue;

            if (!DateTime.TryParseExact(timeObj?.ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                continue;

            if (!double.TryParse(priceObj?.ToString(), out var price) ||
                !double.TryParse(highObj?.ToString(), out var high) ||
                !double.TryParse(lowObj?.ToString(), out var low))
                continue;

            // Normalize UTC entries to local time
            if (timeObj?.ToString()?.EndsWith("UTC") == true)
                timestamp = timestamp.ToLocalTime();

            context.PriceHistory.Add(new StockPrice
            {
                Symbol = symbol,
                Price = price,
                High = high,
                Low = low,
                Timestamp = timestamp
            });
        }

        context.PriceHistory = context.PriceHistory
            .OrderBy(p => p.Timestamp)
            .ToList();

        if (context.PriceHistory.Count > 0)
            context.CurrentPrice = context.PriceHistory[^1];

        return context;
    }
}
