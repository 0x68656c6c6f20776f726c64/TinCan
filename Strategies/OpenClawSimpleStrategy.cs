using TinCan.Models;
using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawSimpleStrategy : OpenClawStrategy
{
    public override string Name => "OpenClawSimpleStrategy";

    public OpenClawSimpleStrategy(OpenClawService openClawService)
        : base(openClawService)
    {
    }

    protected override async Task<Signal> BuildSignalFromResponse(OpenClawResponse? response)
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
