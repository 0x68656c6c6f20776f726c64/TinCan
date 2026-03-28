using Newtonsoft.Json;
using TinCan.Models;

namespace TinCan.Tests.Integration.Helpers;

/// <summary>
/// Helper class for setting up test environment and loading configuration for integration tests.
/// </summary>
public class FinnhubServiceSetupHelper
{
    /// <summary>
    /// Sets up the test environment and retrieves the Finnhub API key from settings file or environment variable.
    /// </summary>
    /// <returns>The Finnhub API key, or null if not configured.</returns>
    public static string? SetupAndGetApiKey()
    {
        // Try to find settings.json by traversing up from test output directory
        var dir = Directory.GetCurrentDirectory();
        string? settingsPath = null;

        // Search up to 5 levels for settings.json in repo root or stock_bot/
        for (int i = 0; i < 5; i++)
        {
            // Check root settings.json
            var rootCandidate = Path.Combine(dir, "settings.json");
            if (File.Exists(rootCandidate))
            {
                settingsPath = rootCandidate;
                break;
            }
            
            // Check stock_bot/settings.json (where it lives)
            var botCandidate = Path.Combine(dir, "stock_bot", "settings.json");
            if (File.Exists(botCandidate))
            {
                settingsPath = botCandidate;
                break;
            }
            
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        // Also check environment variable
        var envKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY");

        string? apiKey = null;

        // Try to load from settings file first
        if (!string.IsNullOrEmpty(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                apiKey = settings?.Providers?.Finnhub?.ApiKey;
            }
            catch { }
        }

        // Fallback to environment variable
        if (string.IsNullOrEmpty(apiKey) || apiKey == "your_finnhub_api_key")
        {
            apiKey = envKey;
        }

        return apiKey;
    }
}
