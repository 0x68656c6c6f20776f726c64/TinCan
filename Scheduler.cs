using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using TinCan.Models;
using TinCan.Services;

namespace TinCan;

public class Scheduler
{
    private readonly Settings _settings;
    private readonly string _projectDir;
    private readonly FinnhubService _finnhubService;
    private readonly StockFileService _stockFileService;

    public Scheduler(Settings settings, string projectDir)
    {
        _settings = settings;
        _projectDir = projectDir;

        var finnhub = _settings.Providers?.Finnhub;

        _finnhubService = new FinnhubService(finnhub?.ApiKey ?? "", finnhub?.Timeout ?? 5);
        // AppContext.BaseDirectory points to bin/ during dotnet run, use current dir instead
        var baseDir = Directory.GetCurrentDirectory();
        _stockFileService = new StockFileService(baseDir);
    }

    public int IntervalMinutes => _settings.Scheduler?.IntervalMinutes ?? 5;

    public async Task RunProvidersAsync()
    {
        var finnhub = _settings.Providers?.Finnhub;
        if (finnhub == null || !finnhub.Enabled)
        {
            Console.WriteLine("[INFO] Finnhub provider is disabled or API key not set");
            return;
        }

        var lookup = _stockFileService.LoadLookup();
        var stocks = _stockFileService.GetEnabledStocks(lookup);

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
                var price = await _finnhubService.FetchPriceAsync(symbol);
                if (price != null)
                {
                    _stockFileService.UpdateStockFile(symbol, price.Price, price.High, price.Low, lookup);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {symbol}: {ex.Message}");
            }
        }
    }
}
