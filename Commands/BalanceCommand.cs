using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Interfaces;

namespace TinCan.Commands;

public static class BalanceCommand
{
    public static void Execute(CommandLineApplication app)
    {
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
                var balance = broker.GetBalanceAsync().GetAwaiter().GetResult();

                Console.WriteLine("\nAccount Balance:");
                Console.WriteLine($"  Cash:          ${balance.Cash:F2}");
                Console.WriteLine($"  Equity:        ${balance.Equity:F2}");

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
