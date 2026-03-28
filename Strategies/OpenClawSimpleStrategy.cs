using TinCan.Models;
using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawSimpleStrategy : OpenClawStrategy
{
    public OpenClawSimpleStrategy(IOpenClawService openClawService) 
        : base(openClawService)
    {
    }

    public override string Name => "OpenClawSimpleStrategy";

    public override async Task<Signal> GenerateAsync(MarketContext context)
    {
        try
        {
            var result = await _openClawService.GetStrategySuggestionAsync(context);
            
            if (result == null)
            {
                return CreateSignal(SignalType.Hold, "OpenClaw returned no result", 0.0);
            }

            var signalType = result.Suggestion.ToLowerInvariant() switch
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
