using System.IO;
using Newtonsoft.Json;
using TinCan.Models;

namespace TinCan.Infrastructure;

public static class SettingsLoader
{
    private const string DefaultSettingsFile = "settings.json";
    private static readonly string[] DefaultSettingsPaths =
    [
        DefaultSettingsFile,
        Path.Combine("stock_bot", "settings.json"),
        Path.Combine("..", "stock_bot", "settings.json")
    ];

    public static Settings Load(string? settingsPath = null)
    {
        var path = settingsPath ?? ResolveDefaultSettingsPath();

        if (!File.Exists(path))
        {
            Console.WriteLine($"[WARN] {path} not found, using defaults.");
            return new Settings();
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }

    private static string ResolveDefaultSettingsPath()
    {
        foreach (var candidate in DefaultSettingsPaths)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return DefaultSettingsFile;
    }
}
