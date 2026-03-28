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

    public override async Task<Signal> GenerateAsync(MarketContext context)
    {
        try
        {
            var result = await _openClawService.GetStrategySuggestionAsync(context);
            
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
        catch (Exception ex)
        {
            return CreateSignal(SignalType.Hold, $"OpenClaw error: {ex.Message}", 0.0);
        }
    }
}
