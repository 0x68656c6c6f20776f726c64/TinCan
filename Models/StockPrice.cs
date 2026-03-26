namespace TinCan.Models;

public class StockPrice
{
    public string Symbol { get; set; } = "";
    public double Price { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public DateTime Timestamp { get; set; }
}
