using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Strategies;

namespace TinCan.Services;

public class SignalExecutor
{
    private readonly IStrategy _strategy;
    private readonly IBrokerService _broker;
    private readonly IMarketDataProviderService _marketData;

    public SignalExecutor(IStrategy strategy, IBrokerService broker, IMarketDataProviderService marketData)
    {
        _strategy = strategy;
        _broker = broker;
        _marketData = marketData;
    }

    public async Task<ExecutionResult> ExecuteAsync(Signal signal)
    {
        if (signal.Type == SignalType.Hold)
        {
            return new ExecutionResult
            {
                Success = true,
                ErrorMessage = null,
                Order = null
            };
        }

        var quantity = signal.Quantity;
        if (quantity <= 0)
        {
            return new ExecutionResult
            {
                Success = false,
                ErrorMessage = $"Invalid quantity: {quantity}",
                Order = null
            };
        }

        var side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell;
        var orderType = signal.OrderType ?? OrderType.Market;
        var limitPrice = signal.LimitPrice;

        try
        {
            var result = await _broker.PlaceOrderAsync(
                signal.Symbol,
                quantity,
                side,
                orderType,
                limitPrice
            );

            return new ExecutionResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                Order = result.Order
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Order = null
            };
        }
    }
}
