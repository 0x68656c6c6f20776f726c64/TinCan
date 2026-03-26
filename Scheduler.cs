using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using TinCan.Models;
using TinCan.Services;

namespace TinCan;

public class Scheduler
{
    private readonly Settings _settings;
    private readonly string _projectDir;

    public Scheduler(Settings settings, string projectDir)
    {
        _settings = settings;
        _projectDir = projectDir;
    }

    public int IntervalMinutes => _settings.Scheduler?.IntervalMinutes ?? 5;

    private static Settings LoadSettings(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[WARN] settings.json not found at {path}");
            return new Settings();
        }
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }

    public void RunProviders()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running providers...");
        RunFinnhub().Wait();
    }

    private async Task RunFinnhub()
    {
        var finnhub = _settings.Providers?.Finnhub;
        if (finnhub == null || !finnhub.Enabled)
            return;

        var service = new FinnhubService(finnhub, _projectDir);
        await service.RunAsync();
    }
}
