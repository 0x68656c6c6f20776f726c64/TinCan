using McMaster.Extensions.CommandLineUtils;
using TinCan.Interfaces;

namespace TinCan.Commands;

public static class FetchCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var intervalOpt = app.Option<int>("--interval", "Fetch interval in minutes", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var settings = Infrastructure.SettingsLoader.Load(settingsOpt.Value());
            var projectDir = Directory.GetCurrentDirectory();

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
            var scheduler = new Scheduler(settings, marketData, projectDir);

            var interval = intervalOpt.HasValue() ? intervalOpt.ParsedValue : 5;
            Console.WriteLine($"[INFO] Interval: {interval} minute(s)");
            Console.WriteLine("[INFO] Press Ctrl+C to stop");
            Console.WriteLine("==================================================");

            scheduler.RunAsync().GetAwaiter().GetResult();
            return 0;
        });
    }
}
