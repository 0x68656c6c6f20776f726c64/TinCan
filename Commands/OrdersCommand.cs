using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Interfaces;
using TinCan.Models;

namespace TinCan.Commands;

public static class OrdersCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var openOpt = app.Option<bool>("--open", "Show only open orders", CommandOptionType.NoValue);
        var symbolOpt = app.Option<string>("--symbol", "Filter by symbol", CommandOptionType.SingleValue);
        var providerOpt = app.Option<string>("--provider", "Broker provider (paper, alpaca)", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var settings = SettingsLoader.Load(settingsOpt.Value());
            var projectDir = Directory.GetCurrentDirectory();

            IMarketDataProviderService marketData;
            try
            {
                marketData = MarketDataProviderFactory.Create(settings);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }

            IBrokerService broker;
            try
            {
                broker = BrokerFactory.Create(settings, marketData, projectDir);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }

            var symbol = symbolOpt.Value();
            var resolvedProvider = settings.Providers?.Broker ?? "paper";
            Console.WriteLine($"[INFO] Provider: {resolvedProvider}");

            try
            {
                var orders = openOpt.HasValue()
                    ? broker.GetOpenOrdersAsync(symbol ?? "").GetAwaiter().GetResult()
                    : broker.GetOrderHistoryAsync(symbol ?? "").GetAwaiter().GetResult();

                if (orders.Count == 0)
                {
                    Console.WriteLine("[INFO] No orders found.");
                    return 0;
                }

                Console.WriteLine("\n{0,-40} {1,-10} {2,-6} {3,-6} {4,-8} {5,-12} {6,-12}", 
                    "OrderId", "Symbol", "Side", "Qty", "Type", "Status", "FillPrice");
                Console.WriteLine(new string('-', 100));

                foreach (var order in orders)
                {
                    Console.WriteLine("{0,-40} {1,-10} {2,-6} {3,-6} {4,-8} {5,-12} {6,-12}", 
                        order.Id, order.Symbol, order.Side.ToString(), order.Quantity, order.Type.ToString(), 
                        order.Status.ToString(), order.FillPrice?.ToString("F2") ?? "N/A");
                }

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
