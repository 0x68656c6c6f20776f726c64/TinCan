using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Models;

namespace TinCan.Commands;

public static class OrderCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var orderIdArg = app.Argument<string>("orderId", "Order ID");
        var providerOpt = app.Option<string>("--provider", "Broker provider", CommandOptionType.SingleValue);
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

            var resolvedProvider = settings.Providers?.Broker ?? "alpaca";
            Console.WriteLine($"[INFO] Provider: {resolvedProvider}");

            try
            {
                // Get all open orders and find the specific one
                var openOrders = broker.GetOpenOrdersAsync("").GetAwaiter().GetResult();
                var order = openOrders.FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    Console.WriteLine($"[ERROR] Order '{orderId}' not found.");
                    return 1;
                }

                Console.WriteLine($"\nOrder Details:");
                Console.WriteLine($"  Order ID:    {order.Id}");
                Console.WriteLine($"  Symbol:      {order.Symbol}");
                Console.WriteLine($"  Side:        {order.Side}");
                Console.WriteLine($"  Quantity:    {order.Quantity}");
                Console.WriteLine($"  Type:        {order.Type}");
                Console.WriteLine($"  Status:      {order.Status}");
                Console.WriteLine($"  Limit Price: {order.LimitPrice?.ToString("F2") ?? "N/A"}");
                Console.WriteLine($"  Fill Price:  {order.FillPrice?.ToString("F2") ?? "N/A"}");
                Console.WriteLine($"  Created:     {order.CreatedAt}");
                if (order.FilledAt.HasValue)
                    Console.WriteLine($"  Filled:      {order.FilledAt}");

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
