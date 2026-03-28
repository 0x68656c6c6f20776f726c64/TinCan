using McMaster.Extensions.CommandLineUtils;

namespace TinCan.Commands;

public static class PositionsCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var providerOpt = app.Option<string>("--provider", "Broker provider", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var resolvedProvider = Infrastructure.ProviderResolver.Resolve(providerOpt.Value(), null);
            Console.WriteLine($"[INFO] Provider: {resolvedProvider}");
            Console.WriteLine("[ERROR] This command requires the Execution Layer (Story #13) to be implemented.");
            return 1;
        });
    }
}
