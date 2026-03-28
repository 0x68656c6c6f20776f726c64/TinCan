using System.Threading.Tasks;
using TinCan.Models;

namespace TinCan.Interfaces;

public interface IMarketDataProviderService
{
    Task<StockPrice?> FetchPriceAsync(string symbol);
    Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, string resolution, DateTime fromUtc, DateTime toUtc);
}
