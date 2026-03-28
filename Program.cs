using System;
using System.IO;
using Newtonsoft.Json;
using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;

namespace TinCan;

class Program
{
    private static string SETTINGS_FILE = "settings.json";

    static Settings LoadSettings()
    {
        if (!File.Exists(SETTINGS_FILE))
        {
            Console.WriteLine($"[WARN] settings.json not found at {SETTINGS_FILE}");
            return new Settings();
        }
        var json = File.ReadAllText(SETTINGS_FILE);
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }

    static IMarketDataProviderService CreateMarketDataProvider(Settings settings)
    {
        var finnhub = settings.Providers?.Finnhub;
        if (finnhub?.Enabled == true && !string.IsNullOrWhiteSpace(finnhub.ApiKey))
        {
            return new FinnhubService(finnhub.ApiKey, finnhub.Timeout);
        }

        throw new InvalidOperationException("No enabled market data provider is configured.");
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" TinCan - Starting up");
        Console.WriteLine("==================================================");

        var settings = LoadSettings();
        var projectDir = Directory.GetCurrentDirectory();
        var marketDataProviderService = CreateMarketDataProvider(settings);
        var scheduler = new Scheduler(settings, marketDataProviderService, projectDir);

        Console.WriteLine($" Interval: {scheduler.IntervalMinutes} minute(s)");
        Console.WriteLine($" Settings: {Path.GetFullPath(SETTINGS_FILE)}");
        Console.WriteLine("==================================================");

        await scheduler.RunAsync();
    }
}
