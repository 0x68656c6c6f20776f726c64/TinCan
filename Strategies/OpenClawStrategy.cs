using TinCan.Models;
using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawStrategy : StrategyBase
{
    protected readonly IOpenClawService _openClawService;

    public OpenClawStrategy(IOpenClawService openClawService)
    {
        _openClawService = openClawService;
    }

    public override string Name => "OpenClawStrategy";

    public override Task<Signal> GenerateAsync(MarketContext context)
    {
        // Default implementation - subclasses should override
        return Task.FromResult(CreateSignal(SignalType.Hold, "Not implemented", 0.0));
    }

    protected virtual async Task<Signal> GenerateSignalAsync(MarketContext context)
    {
        var result = await _openClawService.GetStrategySuggestionAsync(context);
        return await BuildSignalFromResponseAsync(result);
    }

    protected virtual async Task<Signal> BuildSignalFromResponseAsync(OpenClawResult? result)
    {
        if (result == null || string.IsNullOrEmpty(result.Suggestion))
        {
            return await Task.FromResult(CreateSignal(SignalType.Hold, "No response from OpenClaw", 0.0));
        }

        var signalType = result.Suggestion.ToLowerInvariant() switch
        {
            "buy" => SignalType.Buy,
            "sell" => SignalType.Sell,
            _ => SignalType.Hold
        };

        return await Task.FromResult(CreateSignal(signalType, result.Reason ?? "", result.Confidence));
    }
}
