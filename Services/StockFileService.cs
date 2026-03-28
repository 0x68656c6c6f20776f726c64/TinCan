using System;
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
}
