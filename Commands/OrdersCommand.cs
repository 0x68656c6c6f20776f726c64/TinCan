using McMaster.Extensions.CommandLineUtils;

namespace TinCan.Commands;

public static class OrdersCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var openOpt = app.Option<bool>("--open", "Show only open orders", CommandOptionType.NoValue);
        var symbolOpt = app.Option<string>("--symbol", "Filter by symbol", CommandOptionType.SingleValue);
        var providerOpt = app.Option<string>("--provider", "Broker provider (paper, alpaca)", CommandOptionType.SingleValue);

        app.HelpOption("-?|-h|--help");
        app.OnExecute(() =>
        {
            var resolvedProvider = Infrastructure.ProviderResolver.Resolve(providerOpt.Value(), null);
            Console.WriteLine($"[INFO] Provider: {resolvedProvider}");

            if (resolvedProvider == "paper")
            {
                Console.WriteLine("[ERROR] Paper broker order listing requires the Execution Layer (Story #13).");
                return 1;
            }

            Console.WriteLine("[ERROR] No real broker provider configured.");
            return 1;
        });
    }
}
