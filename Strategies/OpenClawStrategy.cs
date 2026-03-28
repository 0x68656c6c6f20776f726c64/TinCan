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
        try
        {
            var result = _openClawService.GetStrategySuggestionAsync(context).GetAwaiter().GetResult();
            
            if (result == null)
            {
                return CreateSignal(SignalType.Hold, "OpenClaw returned no result", 0.0);
            }

            var signalType = result.Suggestion.ToLower() switch
            {
                "buy" => SignalType.Buy,
                "sell" => SignalType.Sell,
                _ => SignalType.Hold
            };

            return CreateSignal(signalType, result.Reason, result.Confidence);
        }
        catch (Exception ex)
        {
            return CreateSignal(SignalType.Hold, $"OpenClaw error: {ex.Message}", 0.0);
        }
    }
}
