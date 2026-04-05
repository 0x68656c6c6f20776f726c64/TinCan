using Newtonsoft.Json;
using TinCan.Models;

namespace TinCan.Tests.Integration.Helpers;

/// <summary>
/// Helper class for setting up test environment and loading configuration for integration tests.
/// </summary>
public class FinnhubServiceSetupHelper
{
    private static readonly string[] ProxyEnvironmentVariables =
    [
        "ALL_PROXY",
        "HTTP_PROXY",
        "HTTPS_PROXY",
        "GIT_HTTP_PROXY",
        "GIT_HTTPS_PROXY"
    ];

    /// <summary>
    /// Sets up the test environment and retrieves the Finnhub API key from settings file or environment variable.
    /// </summary>
    /// <returns>The Finnhub API key, or null if not configured.</returns>
    public static string? SetupAndGetApiKey()
    {
        // Try to find settings.json by traversing up from test output directory
        var dir = Directory.GetCurrentDirectory();
        string? settingsPath = null;

        // Search upward from the test output directory until we reach the repo root.
        for (int i = 0; i < 8; i++)
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

            // Check sibling ../stock_bot/settings.json
            var siblingBotCandidate = Path.GetFullPath(Path.Combine(dir, "..", "stock_bot", "settings.json"));
            if (File.Exists(siblingBotCandidate))
            {
                settingsPath = siblingBotCandidate;
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

    public static Dictionary<string, string?> ClearProxyEnvironmentVariables()
    {
        var originalValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in ProxyEnvironmentVariables)
        {
            originalValues[variable] = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, null);
        }

        return originalValues;
    }

    public static void RestoreEnvironmentVariables(Dictionary<string, string?> values)
    {
        foreach (var pair in values)
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    }
}
