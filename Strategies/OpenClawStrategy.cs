using TinCan.Models;
using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawStrategy : StrategyBase
{
    public override string Name => "OpenClawStrategy";

    protected readonly OpenClawService _openClawService;

    public OpenClawStrategy(OpenClawService openClawService)
    {
        _openClawService = openClawService;
    }

    public override async Task<Signal> Generate(MarketContext context)
    {
        if (context.CurrentPrice == null)
        {
            return CreateSignal(SignalType.Hold, "No current price available", 0.0);
        }

        try
        {
            var response = await _openClawService.GetTradingSignalAsync(
                context.Symbol,
                context.CurrentPrice.Price,
                context.CurrentPrice.High,
                context.CurrentPrice.Low,
                context.CurrentPrice.Timestamp
            );

            return await BuildSignalFromResponse(response);
        }
        catch
        {
            return CreateSignal(SignalType.Hold, "Error calling OpenClaw service", 0.0);
        }
    }

    protected virtual async Task<Signal> BuildSignalFromResponse(OpenClawResponse? response)
    {
        if (response == null || string.IsNullOrEmpty(response.Suggestion))
        {
            return CreateSignal(SignalType.Hold, "No response from OpenClaw", 0.1);
        }

        var signalType = response.Suggestion.ToLowerInvariant() switch
        {
            "buy" => SignalType.Buy,
            "sell" => SignalType.Sell,
            _ => SignalType.Hold
        };

        var reason = string.IsNullOrEmpty(response.Reason)
            ? $"OpenClaw suggested {response.Suggestion}"
            : response.Reason;

        return CreateSignal(signalType, reason, response.Confidence);
    }
}
