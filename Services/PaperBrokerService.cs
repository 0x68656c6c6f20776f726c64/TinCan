using TinCan.Interfaces;
using TinCan.Models;

namespace TinCan.Services;

public class PaperBrokerService : IBrokerService
{
    private double _cash;
    private readonly Dictionary<string, Position> _positions = new();
    private readonly List<Order> _orders = new();
    private readonly List<Order> _orderHistory = new();
    private readonly IMarketDataProviderService _marketData;
    private readonly string _projectDir;
    private const string PaperTradesFile = "paper_trades.json";

    public PaperBrokerService(IMarketDataProviderService marketData, string projectDir, double initialCash = 10000.00)
    {
        _marketData = marketData;
        _projectDir = projectDir;
        _cash = initialCash;
    }

    public Task<BrokerBalance> GetBalanceAsync(string symbol)
    {
        var positionValue = _positions.TryGetValue(symbol.ToUpperInvariant(), out var pos)
            ? pos.Quantity * pos.AvgCost
            : 0;
        var equity = _cash + positionValue;

        return Task.FromResult(new BrokerBalance { Cash = _cash, Equity = equity });
    }

    public async Task<OrderResult> PlaceOrderAsync(string symbol, int quantity, OrderSide side, OrderType type, double? limitPrice = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            Symbol = symbol.ToUpperInvariant(),
            Quantity = quantity,
            Side = side,
            Type = type,
            LimitPrice = limitPrice,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _orders.Add(order);

        try
        {
            switch (type)
            {
                case OrderType.Market:
                    await FillMarketOrderAsync(order);
                    break;
                case OrderType.Limit:
                    // Limit orders are stored and filled when price crosses limit
                    // For now, check if we can fill immediately
                    var currentPrice = await GetCurrentPriceAsync(symbol);
                    if (currentPrice == null)
                    {
                        order.Status = OrderStatus.Rejected;
                        order.FilledAt = DateTime.Now;
                        SavePaperTrades();
                        return new OrderResult { Success = false, ErrorMessage = "Could not fetch current price", Order = order };
                    }
                    var shouldFill = side == OrderSide.Buy
                        ? currentPrice <= limitPrice
                        : currentPrice >= limitPrice;
                    if (shouldFill)
                        FillOrder(order, currentPrice.Value);
                    break;
            }
        }
        catch (Exception ex)
        {
            order.Status = OrderStatus.Rejected;
            SavePaperTrades();
            return new OrderResult { Success = false, ErrorMessage = ex.Message, Order = order };
        }

        SavePaperTrades();
        return new OrderResult { Success = order.Status == OrderStatus.Filled, Order = order };
    }

    public Task<List<Order>> GetOpenOrdersAsync(string symbol)
    {
        var openOrders = _orders
            .Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) && o.Status == OrderStatus.Pending)
            .ToList();
        return Task.FromResult(openOrders);
    }

    public Task<List<Order>> GetOrderHistoryAsync(string symbol, int limit = 50)
    {
        var history = _orderHistory
            .Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(history);
    }

    public Task<bool> CancelOrderAsync(string orderId)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
            return Task.FromResult(false);

        if (order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Cancelled;
            order.FilledAt = DateTime.Now;
            SavePaperTrades();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private async Task<double?> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            var price = await _marketData.FetchPriceAsync(symbol);
            return price?.Price;
        }
        catch
        {
            return null;
        }
    }

    private async Task FillMarketOrderAsync(Order order)
    {
        var price = await GetCurrentPriceAsync(order.Symbol);
        if (price == null)
        {
            order.Status = OrderStatus.Rejected;
            return;
        }
        FillOrder(order, price.Value);
    }

    private void FillOrder(Order order, double fillPrice)
    {
        order.FillPrice = fillPrice;
        order.Status = OrderStatus.Filled;
        order.FilledAt = DateTime.Now;

        var positionKey = order.Symbol.ToUpperInvariant();

        if (order.Side == OrderSide.Buy)
        {
            var cost = order.Quantity * fillPrice;
            if (cost > _cash)
            {
                order.Status = OrderStatus.Rejected;
                return;
            }

            _cash -= cost;

            if (_positions.TryGetValue(positionKey, out var existing))
            {
                var totalQty = existing.Quantity + order.Quantity;
                var totalCost = (existing.Quantity * existing.AvgCost) + (order.Quantity * fillPrice);
                _positions[positionKey] = new Position { Quantity = totalQty, AvgCost = totalCost / totalQty };
            }
            else
            {
                _positions[positionKey] = new Position { Quantity = order.Quantity, AvgCost = fillPrice };
            }
        }
        else // Sell
        {
            if (!_positions.TryGetValue(positionKey, out var position) || position.Quantity < order.Quantity)
            {
                order.Status = OrderStatus.Rejected;
                return;
            }

            var proceeds = order.Quantity * fillPrice;
            _cash += proceeds;

            position.Quantity -= order.Quantity;
            if (position.Quantity == 0)
                _positions.Remove(positionKey);
            else
                _positions[positionKey] = position;
        }

        _orders.Remove(order);
        _orderHistory.Add(order);
    }

    private void SavePaperTrades()
    {
        try
        {
            var dir = Path.Combine(_projectDir, "stock_bot");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, PaperTradesFile);

            var trades = new PaperTradesRecord
            {
                Cash = _cash,
                Positions = _positions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Orders = _orderHistory.ToList()
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(trades, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Log error but don't fail the order
        }
    }

    private class Position
    {
        public int Quantity { get; set; }
        public double AvgCost { get; set; }
    }

    private class PaperTradesRecord
    {
        public double Cash { get; set; }
        public Dictionary<string, Position> Positions { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }
}
