using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;

namespace TinCan.Infrastructure;

public static class MarketDataProviderFactory
{
    public static IMarketDataProviderService Create(Settings settings)
    {
        var finnhub = settings.Providers?.Finnhub;
        if (finnhub?.Enabled != true || string.IsNullOrWhiteSpace(finnhub.ApiKey))
        {
            throw new InvalidOperationException("No enabled market data provider is configured.");
        }
        return new FinnhubService(finnhub.ApiKey, finnhub.Timeout);
    }
}
