using System;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;
using TinCan.Strategies;

namespace TinCan;

public class Scheduler
{
    private readonly Settings _settings;
    private readonly IMarketDataProviderService _marketDataProviderService;
    private readonly StockFileService _stockFileService;
    private readonly IStrategy? _strategy;
    private readonly SignalExecutor? _signalExecutor;
    private bool _historicalDataFetched;

    public Scheduler(Settings settings, IMarketDataProviderService marketDataProviderService, string projectDir)
    {
        _settings = settings;
        _marketDataProviderService = marketDataProviderService;
        _stockFileService = new StockFileService(projectDir);
    }

    public Scheduler(Settings settings, IMarketDataProviderService marketDataProviderService, IStrategy strategy, IBrokerService broker, string projectDir)
    {
        _settings = settings;
        _marketDataProviderService = marketDataProviderService;
        _stockFileService = new StockFileService(projectDir);
        _strategy = strategy;
        _signalExecutor = new SignalExecutor(strategy, broker, marketDataProviderService);
    }

    public int IntervalMinutes => _settings.Scheduler?.IntervalMinutes ?? 5;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await RunProvidersAsync();

        if (_settings.Scheduler?.Historical?.Enabled == true)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(IntervalMinutes), cancellationToken);
                await RunProvidersAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("[INFO] Shutting down...");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }
    }

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
                var price = await _marketDataProviderService.FetchPriceAsync(symbol);
                if (price != null)
                {
                    _stockFileService.UpdateStockFile(symbol, price.Price, price.High, price.Low, lookup);

                    // Execute strategy if configured
                    if (_strategy != null && _signalExecutor != null)
                    {
                        await ExecuteStrategyForSymbolAsync(symbol);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {symbol}: {ex.Message}");
            }
        }
    }

    private async Task ExecuteStrategyForSymbolAsync(string symbol)
    {
        if (_strategy == null || _signalExecutor == null)
            return;

        try
        {
            var context = _stockFileService.LoadMarketContext(symbol);
            if (context.PriceHistory.Count == 0)
            {
                Console.WriteLine($"[WARN] No market context for {symbol}, skipping strategy");
                return;
            }

            var signal = await _strategy.GenerateAsync(context);
            signal.Symbol = symbol; // Ensure signal has the symbol

            Console.WriteLine($"[INFO] Strategy '{_strategy.Name}' generated signal: {signal.Type} for {symbol}");

            var result = await _signalExecutor.ExecuteAsync(signal);

            if (result.Success)
            {
                if (result.Order != null)
                {
                    Console.WriteLine($"[INFO] Order placed: {result.Order.Side} {result.Order.Quantity} {symbol} @ ${result.Order.FillPrice}");
                }
                else
                {
                    Console.WriteLine($"[INFO] Signal {signal.Type} processed (no order needed for Hold)");
                }
            }
            else
            {
                Console.WriteLine($"[ERROR] Order failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Strategy execution failed for {symbol}: {ex.Message}");
        }
    }
}
