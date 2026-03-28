using TinCan.Models;

namespace TinCan.Strategies;

public abstract class StrategyBase : IStrategy
{
    public abstract string Name { get; }

    public abstract Task<Signal> GenerateAsync(MarketContext context);

    public Signal CreateSignal(SignalType type, string reason, double confidence)
    {
        return new Signal
        {
            Type = type,
            Reason = reason,
            Confidence = Math.Clamp(confidence, 0.0, 1.0)
        };
    }
}
