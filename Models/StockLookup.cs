namespace TinCan.Models;

public class StockLookup
{
    public Dictionary<string, StockInfo>? Stocks { get; set; }
}

public class StockInfo
{
    public bool Enabled { get; set; }
    public string? Output { get; set; }
}
