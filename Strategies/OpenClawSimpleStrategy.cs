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
}
