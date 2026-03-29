using McMaster.Extensions.CommandLineUtils;
using TinCan.Interfaces;

namespace TinCan.Commands;

public static class BackfillCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolArg = app.Argument<string>("symbol", "Stock symbol");
        var fromOpt = app.Option<string>("--from", "Start date (YYYY-MM-DD)", CommandOptionType.SingleValue);
        var toOpt = app.Option<string>("--to", "End date (YYYY-MM-DD)", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var symbol = symbolArg.Value;
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Console.WriteLine("[ERROR] Symbol is required.");
                return 1;
            }

            if (!fromOpt.HasValue() || !toOpt.HasValue())
            {
                Console.WriteLine("[ERROR] Both --from and --to dates are required (format: YYYY-MM-DD)");
                return 1;
            }

            if (!DateTime.TryParse(fromOpt.Value(), out var from))
            {
                Console.WriteLine("[ERROR] Invalid --from date format. Use YYYY-MM-DD.");
                return 1;
            }
            if (!DateTime.TryParse(toOpt.Value(), out var to))
            {
                Console.WriteLine("[ERROR] Invalid --to date format. Use YYYY-MM-DD.");
                return 1;
            }

            if (from >= to)
            {
                Console.WriteLine("[ERROR] --from date must be before --to date");
                return 1;
            }

            var settings = Infrastructure.SettingsLoader.Load(settingsOpt.Value());
            var projectDir = Directory.GetCurrentDirectory();

            IMarketDataProviderService marketData;
            try
            {
                marketData = Infrastructure.MarketDataProviderFactory.Create(settings);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }
            var stockFileService = new Services.StockFileService(projectDir);
            var lookup = stockFileService.LoadLookup();

            var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Local).ToUniversalTime();
            var toUtc = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

            Console.WriteLine($"[INFO] Fetching {symbol} from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}...");

            try
            {
                var history = marketData.FetchHistoricalPricesAsync(
                    symbol.ToUpperInvariant(), "D", fromUtc, toUtc).GetAwaiter().GetResult();

                if (history.Count == 0)
                {
                    Console.WriteLine("[WARN] No historical data returned from provider");
                    return 0;
                }

                stockFileService.ReplaceStockFileWithHistory(symbol.ToUpperInvariant(), history, lookup);
                Console.WriteLine($"[INFO] Done. Wrote {history.Count} data points.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }
        });
    }
}
