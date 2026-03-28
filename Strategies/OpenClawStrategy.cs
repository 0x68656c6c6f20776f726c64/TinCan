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
        // Subclasses should override with their specific implementation
        return Task.FromResult(CreateSignal(SignalType.Hold, "Not implemented", 0.0));
    }
}
