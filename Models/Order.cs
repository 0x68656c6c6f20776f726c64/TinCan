namespace TinCan.Models;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; } = "";
    public int Quantity { get; set; }
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public double? LimitPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public double? FillPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? FilledAt { get; set; }
}

public class BrokerBalance
{
    public double Cash { get; set; }
    public double Equity { get; set; }
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}

public class OrderResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}
