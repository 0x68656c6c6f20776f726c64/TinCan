using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using TinCan.Models;

namespace TinCan.Services;

public class FinnhubService
{
    private readonly string _apiKey;
    private readonly int _timeout;
    private readonly HttpClient _http;

    public FinnhubService(string apiKey, int timeout = 5)
    {
        _apiKey = apiKey;
        _timeout = timeout;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(_timeout) };
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
}
