using TinCan.Models;

namespace TinCan.Interfaces;

public interface IBrokerService
{
    Task<BrokerBalance> GetBalanceAsync(string symbol);
    Task<OrderResult> PlaceOrderAsync(string symbol, int quantity, OrderSide side, OrderType type, double? limitPrice = null);
    Task<List<Order>> GetOpenOrdersAsync(string symbol);
    Task<List<Order>> GetOrderHistoryAsync(string symbol, int limit = 50);
    Task<bool> CancelOrderAsync(string orderId);
}
