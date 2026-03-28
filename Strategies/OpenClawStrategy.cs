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

    public override Signal Generate(MarketContext context)
    {
        // Default implementation - subclasses should override
        return CreateSignal(SignalType.Hold, "Not implemented", 0.0);
    }

    protected virtual async Task<Signal> GenerateSignalAsync(MarketContext context)
    {
        var result = await _openClawService.GetStrategySuggestionAsync(context);
        return BuildSignalFromResponse(result);
    }

    protected virtual Signal BuildSignalFromResponse(OpenClawResult? result)
    {
        if (result == null || string.IsNullOrEmpty(result.Suggestion))
        {
            return CreateSignal(SignalType.Hold, "No response from OpenClaw", 0.0);
        }

        var signalType = result.Suggestion.ToLowerInvariant() switch
        {
            "buy" => SignalType.Buy,
            "sell" => SignalType.Sell,
            _ => SignalType.Hold
        };

        return CreateSignal(signalType, result.Reason ?? "", result.Confidence);
    }
}
