using System.Diagnostics;
using System.Text.Json;
using TinCan.Models;

namespace TinCan.Services;

public interface IOpenClawService
{
    Task<OpenClawResponse?> GetStrategySuggestionAsync(MarketContext context, CancellationToken cancellationToken = default);
}

public class OpenClawService : IOpenClawService
{
    private readonly string _gatewayToken;
    private readonly int _gatewayPort;
    private readonly string _agentId;

    public OpenClawService(string gatewayToken, int gatewayPort = 18789, string agentId = "main")
    {
        _gatewayToken = gatewayToken;
        _gatewayPort = gatewayPort;
        _agentId = agentId;
    }

    public async Task<OpenClawResponse?> GetStrategySuggestionAsync(MarketContext context, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(context);
        var result = await ExecuteOpenClawAsync(prompt, cancellationToken);
        
        if (string.IsNullOrEmpty(result))
            return null;
            
        return ParseResponse(result);
    }

    private string BuildPrompt(MarketContext context)
    {
        var price = context.CurrentPrice;
        var history = context.PriceHistory;
        
        var latestPrices = history.Count > 0 
            ? string.Join(", ", history.TakeLast(5).Select(p => $"{p.Symbol}@{p.Price}"))
            : (price != null ? $"{price.Symbol}@{price.Price}" : "N/A");

        return $@"You are a stock trading strategy advisor. Based on the following market data, respond with ONLY a JSON object in this exact format:
{{""suggestion"": ""buy"" or ""sell"" or ""hold"", ""confidence"": 0.0 to 1.0, ""reason"": ""brief explanation""}}

Current Symbol: {context.Symbol}
Current Price: {(price != null ? price.Price.ToString() : "N/A")}
Latest Prices: {latestPrices}

Respond with ONLY the JSON object, no other text.";
    }

    private async Task<string> ExecuteOpenClawAsync(string prompt, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "openclaw",
            Arguments = $"agent --agent {_agentId} --json --message \"{prompt.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return string.Empty;

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        
        return output.Trim();
    }

    private OpenClawResponse? ParseResponse(string response)
    {
        try
        {
            // Try to extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new OpenClawResponse
                {
                    Suggestion = root.TryGetProperty("suggestion", out var suggestion) 
                        ? suggestion.GetString()?.ToLowerInvariant() ?? "hold" 
                        : "hold",
                    Confidence = root.TryGetProperty("confidence", out var confidence) 
                        ? confidence.GetDouble() 
                        : 0.5,
                    Reason = root.TryGetProperty("reason", out var reason) 
                        ? reason.GetString() ?? "" 
                        : ""
                };
            }
        }
        catch
        {
            // Parse failed
        }

        return null;
    }
}
