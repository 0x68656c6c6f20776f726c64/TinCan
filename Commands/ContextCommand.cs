using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace TinCan.Commands;

public static class ContextCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolArg = app.Argument<string>("symbol", "Stock symbol");
        var jsonOpt = app.Option<bool>("--json", "Output as JSON", CommandOptionType.NoValue);
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

            var projectDir = Directory.GetCurrentDirectory();
            var stockFileService = new Services.StockFileService(projectDir);

            var marketContext = stockFileService.LoadMarketContext(symbol.ToUpperInvariant());

            if (marketContext.PriceHistory.Count == 0)
            {
                Console.WriteLine($"[WARN] No data found for {symbol}");
                return 1;
            }

            if (jsonOpt.HasValue())
                Console.WriteLine(JsonConvert.SerializeObject(marketContext, Formatting.Indented));
            else
            {
                Console.WriteLine($"Symbol:       {marketContext.Symbol}");
                Console.WriteLine($"Data points:  {marketContext.PriceHistory.Count}");
                if (marketContext.CurrentPrice != null)
                    Console.WriteLine($"Current:      ${marketContext.CurrentPrice.Price} " +
                        $"(H: ${marketContext.CurrentPrice.High}, L: ${marketContext.CurrentPrice.Low})");
            }
            return 0;
        });
    }
}
