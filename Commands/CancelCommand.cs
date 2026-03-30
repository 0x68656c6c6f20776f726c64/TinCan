using McMaster.Extensions.CommandLineUtils;

namespace TinCan.Commands;

public static class CancelCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var orderIdArg = app.Argument<string>("orderId", "Order ID to cancel");
        var providerOpt = app.Option<string>("--provider", "Broker provider", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var orderId = orderIdArg.Value;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                Console.WriteLine("[ERROR] Order ID is required.");
                return 1;
            }

            Console.WriteLine("[ERROR] This command requires the Execution Layer (Story #13) to be implemented.");
            return 1;
        });
    }
}
