using System.Text.Json;
using TinCan.Models;

namespace TinCan.Services;

public class OpenClawService
{
    private readonly string _gatewayUrl;
    private readonly string _authToken;

    public OpenClawService(string gatewayUrl, string authToken)
    {
        _gatewayUrl = gatewayUrl;
        _authToken = authToken;
    }

    public async Task<OpenClawResponse?> GetTradingSignalAsync(string symbol, double currentPrice, double high, double low, DateTime timestamp)
    {
        var marketData = new
        {
            symbol,
            currentPrice,
            high,
            low,
            timestamp = timestamp.ToString("o")
        };

        var json = JsonSerializer.Serialize(marketData);

        // Build prompt for OpenClaw agent
        var prompt = $"Given this market data: {json}. " +
            "Give me a trading signal (buy/sell/hold) for this symbol. " +
            "Respond with ONLY valid JSON in this format: {\"suggestion\": \"buy|sell|hold\", \"confidence\": 0.0-1.0, \"reason\": \"...\"}";

        // Escape for shell
        var escapedPrompt = prompt.Replace("\"", "\\\"");

        var command = $"openclaw agent --message \"{escapedPrompt}\" --json --timeout 60";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Extract JSON from output (may have extra text)
            var jsonStart = output.IndexOf('{');
            var jsonEnd = output.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = output.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<OpenClawResponse>(jsonStr);
            }
        }
        catch
        {
            // Return null on error
        }

        return null;
    }
}
