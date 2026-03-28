using System;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinCan.Interfaces;
using TinCan.Models;

namespace TinCan.Services;

public class FinnhubService : IMarketDataProviderService
{
    private readonly string _apiKey;
    private readonly HttpClient _http;

    public FinnhubService(string apiKey, HttpClient http)
    {
        _apiKey = apiKey;
        _http = http;
    }

    public FinnhubService(string apiKey, int timeout = 5)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };
    }

    public async Task<StockPrice?> FetchPriceAsync(string symbol)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_apiKey}";

        var response = await _http.GetStringAsync(url);
        var data = JObject.Parse(response);
        var price = data["c"]?.Value<double>() ?? 0;

        if (price > 0)
        {
            return new StockPrice
            {
                Symbol = symbol,
                Price = price,
                High = data["h"]?.Value<double>() ?? price,
                Low = data["l"]?.Value<double>() ?? price,
                Timestamp = DateTime.Now
            };
        }
        return null;
    }

    public async Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, string resolution, DateTime fromUtc, DateTime toUtc)
    {
        var fromUnix = new DateTimeOffset(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var toUnix = new DateTimeOffset(DateTime.SpecifyKind(toUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var url = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution={resolution}&from={fromUnix}&to={toUnix}&token={_apiKey}";

        var response = await _http.GetStringAsync(url);
        var data = JObject.Parse(response);

        if (!string.Equals(data["s"]?.Value<string>(), "ok", StringComparison.OrdinalIgnoreCase))
            return [];

        var closes = data["c"]?.Values<double>().ToList() ?? [];
        var highs = data["h"]?.Values<double>().ToList() ?? [];
        var lows = data["l"]?.Values<double>().ToList() ?? [];
        var timestamps = data["t"]?.Values<long>().ToList() ?? [];

        var count = new[] { closes.Count, highs.Count, lows.Count, timestamps.Count }.Min();
        var prices = new List<StockPrice>(count);

        for (var i = 0; i < count; i++)
        {
            prices.Add(new StockPrice
            {
                Symbol = symbol,
                Price = closes[i],
                High = highs[i],
                Low = lows[i],
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime
            });
        }

        return prices;
    }
}
