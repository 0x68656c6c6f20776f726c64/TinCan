using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Models;

namespace TinCan.Commands;

public static class PositionsCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolOpt = app.Option<string>("--symbol", "Filter by symbol", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var settings = SettingsLoader.Load(settingsOpt.Value());

            IBrokerService broker;
            try
            {
                broker = BrokerFactory.Create(settings);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }

            var provider = settings.Providers?.Broker ?? "alpaca";
            Console.WriteLine($"[INFO] Provider: {provider}");

            try
            {
                var symbol = symbolOpt.Value();
                var positions = broker.GetPositionsAsync(symbol).GetAwaiter().GetResult();

                if (positions.Count == 0)
                {
                    Console.WriteLine("[INFO] No positions found.");
                    return 0;
                }

                Console.WriteLine("\n{0,-10} {1,-8} {2,-12} {3,-14} {4,-14}", "Symbol", "Qty", "Avg Cost", "Market Value", "Unrealized P&L");
                Console.WriteLine(new string('-', 70));

                foreach (var pos in positions)
                {
                    var pnlStr = pos.UnrealizedPnL >= 0 ? $"+${pos.UnrealizedPnL:F2}" : $"-${Math.Abs(pos.UnrealizedPnL):F2}";
                    Console.WriteLine("{0,-10} {1,-8} ${2:F2}      ${3:F2}     {4}", pos.Symbol, pos.Quantity, pos.AvgCost, pos.MarketValue, pnlStr);
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
