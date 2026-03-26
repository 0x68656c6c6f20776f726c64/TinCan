using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using TinCan.Models;

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

    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" TinCan - Starting up");
        Console.WriteLine("==================================================");

        var settings = LoadSettings();
        var projectDir = Directory.GetCurrentDirectory();
        var scheduler = new Scheduler(settings, projectDir);
        var intervalMinutes = scheduler.IntervalMinutes;

        Console.WriteLine($" Interval: {intervalMinutes} minute(s)");
        Console.WriteLine($" Settings: {Path.GetFullPath(SETTINGS_FILE)}");
        Console.WriteLine("==================================================");

        // Run once immediately
        await scheduler.RunProvidersAsync();

        // Main loop
        while (true)
        {
            try
            {
                Thread.Sleep(TimeSpan.FromMinutes(intervalMinutes));
                await scheduler.RunProvidersAsync();
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("[INFO] Shutting down...");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }
    }
}
