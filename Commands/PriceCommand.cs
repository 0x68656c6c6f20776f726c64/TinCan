using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using TinCan.Interfaces;

namespace TinCan.Commands;

public static class PriceCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolArg = app.Argument<string>("symbol", "Stock symbol (e.g. U, AAPL)");
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

            var settings = Infrastructure.SettingsLoader.Load(settingsOpt.Value());

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

            try
            {
                var price = marketData.FetchPriceAsync(symbol.ToUpperInvariant()).GetAwaiter().GetResult();
                if (price == null)
                {
                    Console.WriteLine($"[ERROR] Could not fetch price for {symbol}");
                    return 1;
                }

                if (jsonOpt.HasValue())
                    Console.WriteLine(JsonConvert.SerializeObject(price, Formatting.Indented));
                else
                    Console.WriteLine($"{symbol.ToUpperInvariant()}: ${price.Price} (H: ${price.High}, L: ${price.Low})");
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
