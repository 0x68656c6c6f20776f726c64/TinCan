namespace TinCan.Models;

public class Position
{
    public string Symbol { get; set; } = "";
    public int Quantity { get; set; }
    public double AvgCost { get; set; }
    public double MarketValue { get; set; }
    public double UnrealizedPnL { get; set; }
}