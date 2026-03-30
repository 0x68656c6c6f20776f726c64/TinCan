using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Interfaces;

namespace TinCan.Commands;

public static class CancelCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var orderIdArg = app.Argument<string>("orderId", "Order ID to cancel");
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var orderId = orderIdArg.Value;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                Console.WriteLine("[ERROR] Order ID is required.");
                return 1;
            }

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
                Console.WriteLine($"[INFO] Cancelling order: {orderId}");

                var success = broker.CancelOrderAsync(orderId).GetAwaiter().GetResult();

                if (success)
                {
                    Console.WriteLine($"[INFO] Order {orderId} cancelled successfully.");
                    return 0;
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to cancel order {orderId}. Order may have already been filled or cancelled.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 1;
            }
        });
    }
}
