using McMaster.Extensions.CommandLineUtils;
using TinCan.Factory;
using TinCan.Infrastructure;
using TinCan.Models;

namespace TinCan.Commands;

public static class BuyCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolArg = app.Argument<string>("symbol", "Stock symbol (e.g. AAPL, U)");
        var quantityArg = app.Argument<int>("quantity", "Number of shares to buy");
        var limitPriceOpt = app.Option<double>("--limit", "Limit price for limit order", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var symbol = symbolArg.Value?.ToUpperInvariant();
            var quantity = quantityArg.ParsedValue;

            if (string.IsNullOrWhiteSpace(symbol))
            {
                Console.WriteLine("[ERROR] Symbol is required.");
                return 1;
            }

            if (quantity <= 0)
            {
                Console.WriteLine("[ERROR] Quantity must be greater than 0.");
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
                var orderType = limitPriceOpt.HasValue() ? OrderType.Limit : OrderType.Market;
                var limitPrice = limitPriceOpt.HasValue() ? limitPriceOpt.ParsedValue : (double?)null;

                Console.WriteLine($"[INFO] Placing buy order: {quantity} shares of {symbol}" + 
                    (limitPrice.HasValue ? $" @ ${limitPrice:F2}" : " at market price"));

                var result = broker.PlaceOrderAsync(symbol, quantity, OrderSide.Buy, orderType, limitPrice).GetAwaiter().GetResult();

                if (result.Success && result.Order != null)
                {
                    Console.WriteLine($"[INFO] Order placed successfully!");
                    Console.WriteLine($"  Order ID:    {result.Order.Id}");
                    Console.WriteLine($"  Symbol:      {result.Order.Symbol}");
                    Console.WriteLine($"  Side:        {result.Order.Side}");
                    Console.WriteLine($"  Quantity:    {result.Order.Quantity}");
                    Console.WriteLine($"  Type:        {result.Order.Type}");
                    Console.WriteLine($"  Status:      {result.Order.Status}");
                    if (result.Order.FillPrice.HasValue)
                        Console.WriteLine($"  Fill Price:  ${result.Order.FillPrice:F2}");
                    return 0;
                }
                else
                {
                    Console.WriteLine($"[ERROR] Order failed: {result.ErrorMessage}");
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
