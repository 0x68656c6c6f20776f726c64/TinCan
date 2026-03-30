namespace TinCan.Models;

public enum SignalType
{
    Buy,
    Sell,
    Hold
}

public class Signal
{
    public SignalType Type { get; set; }
    public string Reason { get; set; } = "";
    public double Confidence { get; set; } // 0.0 to 1.0
    public string Symbol { get; set; } = "";
    public int Quantity { get; set; } = 100; // Default quantity for orders
    public OrderType? OrderType { get; set; } // null = market order
    public double? LimitPrice { get; set; }
}
