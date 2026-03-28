using McMaster.Extensions.CommandLineUtils;

namespace TinCan.Commands;

public static class FetchCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var intervalOpt = app.Option<int>("--interval", "Fetch interval in minutes", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        var providerOpt = app.Option<string>("--provider", "Data provider (currently only finnhub)", CommandOptionType.SingleValue);

        app.HelpOption("-?|-h|--help");
        app.OnExecute(() =>
        {
            var settings = Infrastructure.SettingsLoader.Load(settingsOpt.Value());
            var projectDir = Directory.GetCurrentDirectory();

            if (providerOpt.HasValue())
                Console.WriteLine($"[INFO] Provider: {providerOpt.Value()}");

            var finnhub = settings.Providers?.Finnhub;
            if (finnhub?.Enabled != true || string.IsNullOrWhiteSpace(finnhub.ApiKey))
            {
                Console.WriteLine("[ERROR] Finnhub provider is not configured in settings.json");
                return 1;
            }

            var marketData = new Services.FinnhubService(finnhub.ApiKey, finnhub.Timeout);
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
