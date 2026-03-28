using System.IO;
using Newtonsoft.Json;
using TinCan.Models;

namespace TinCan.Infrastructure;

public static class SettingsLoader
{
    private const string DefaultSettingsFile = "settings.json";

    public static Settings Load(string? settingsPath = null)
    {
        var path = settingsPath ?? DefaultSettingsFile;

        if (!File.Exists(path))
        {
            Console.WriteLine($"[WARN] {path} not found, using defaults.");
            return new Settings();
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }
}
