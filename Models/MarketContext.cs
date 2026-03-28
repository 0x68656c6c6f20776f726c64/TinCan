namespace TinCan.Models;

public class MarketContext
{
    public string Symbol { get; set; } = "";
    public StockPrice? CurrentPrice { get; set; }
    public List<StockPrice> PriceHistory { get; set; } = [];
}
