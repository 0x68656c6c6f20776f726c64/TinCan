using System.Net.Http.Json;
using System.Text.Json;
using TinCan.Interfaces;
using TinCan.Models;

namespace TinCan.Services;

public class AlpacaBrokerService : IBrokerService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public AlpacaBrokerService(string apiKey, string secretKey, string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("APCA-API-KEY-ID", apiKey);
        _httpClient.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", secretKey);
    }

    public async Task<BrokerBalance> GetBalanceAsync()
    {
        var response = await _httpClient.GetAsync("/v2/account");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new BrokerBalance
        {
            Cash = ParseDouble(json.GetProperty("cash")),
            Equity = ParseDouble(json.GetProperty("portfolio_value"))
        };
    }

    private static double ParseDouble(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return double.Parse(element.GetString() ?? "0");
        return element.GetDouble();
    }

    public async Task<OrderResult> PlaceOrderAsync(string symbol, int quantity, OrderSide side, OrderType type, double? limitPrice = null)
    {
        var orderRequest = new
        {
            symbol = symbol,
            qty = quantity,
            side = side.ToString().ToLower(),
            type = type.ToString().ToLower(),
            limit_price = limitPrice,
            time_in_force = "day"
        };

        var response = await _httpClient.PostAsJsonAsync("/v2/orders", orderRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return new OrderResult
            {
                Success = false,
                ErrorMessage = error
            };
        }

        var orderJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var order = new Order
        {
            Id = orderJson.GetProperty("id").GetString() ?? "",
            Symbol = orderJson.GetProperty("symbol").GetString() ?? "",
            Quantity = ParseInt(orderJson.GetProperty("qty")),
            Side = Enum.Parse<OrderSide>(orderJson.GetProperty("side").GetString() ?? "Buy", ignoreCase: true),
            Type = Enum.Parse<OrderType>(orderJson.GetProperty("type").GetString() ?? "market", ignoreCase: true),
            Status = ParseOrderStatus(orderJson.GetProperty("status").GetString() ?? ""),
            CreatedAt = DateTime.Parse(orderJson.GetProperty("created_at").GetString() ?? "")
        };

        if (orderJson.TryGetProperty("limit_price", out var limitPriceEl) && limitPriceEl.ValueKind != JsonValueKind.Null)
            order.LimitPrice = ParseDouble(limitPriceEl);

        if (orderJson.TryGetProperty("filled_avg_price", out var fillPriceEl) && fillPriceEl.ValueKind != JsonValueKind.Null)
        {
            order.FillPrice = ParseDouble(fillPriceEl);
            order.FilledAt = DateTime.Now;
        }

        return new OrderResult { Success = true, Order = order };
    }

    public async Task<List<Order>> GetOpenOrdersAsync(string symbol)
    {
        var url = string.IsNullOrEmpty(symbol) ? "/v2/orders?status=open" : $"/v2/orders?status=open&symbols={symbol}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var ordersJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orders = new List<Order>();

        foreach (var item in ordersJson.EnumerateArray())
        {
            orders.Add(ParseOrder(item));
        }

        return orders;
    }

    public async Task<List<Order>> GetOrderHistoryAsync(string symbol, int limit = 50)
    {
        var url = $"/v2/orders?status=closed&limit={limit}";
        if (!string.IsNullOrEmpty(symbol))
            url += $"&symbols={symbol}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var ordersJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orders = new List<Order>();

        foreach (var item in ordersJson.EnumerateArray())
        {
            orders.Add(ParseOrder(item));
        }

        return orders;
    }

    public async Task<bool> CancelOrderAsync(string orderId)
    {
        var response = await _httpClient.DeleteAsync($"/v2/orders/{orderId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<Position>> GetPositionsAsync(string? symbol = null)
    {
        string url;
        if (string.IsNullOrEmpty(symbol))
            url = "/v2/positions";
        else
            url = $"/v2/positions/{symbol}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            // No positions found
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<Position>();
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var positions = new List<Position>();

        if (json.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in json.EnumerateArray())
            {
                positions.Add(ParsePosition(item));
            }
        }
        else
        {
            positions.Add(ParsePosition(json));
        }

        return positions;
    }

    private static Position ParsePosition(JsonElement item)
    {
        return new Position
        {
            Symbol = item.GetProperty("symbol").GetString() ?? "",
            Quantity = ParseInt(item.GetProperty("qty")),
            AvgCost = ParseDouble(item.GetProperty("avg_entry_price")),
            MarketValue = ParseDouble(item.GetProperty("market_value")),
            UnrealizedPnL = ParseDouble(item.GetProperty("unrealized_pl"))
        };
    }

    private static Order ParseOrder(JsonElement item)
    {
        var order = new Order
        {
            Id = item.GetProperty("id").GetString() ?? "",
            Symbol = item.GetProperty("symbol").GetString() ?? "",
            Quantity = ParseInt(item.GetProperty("qty")),
            Side = Enum.Parse<OrderSide>(item.GetProperty("side").GetString() ?? "Buy", ignoreCase: true),
            Type = Enum.Parse<OrderType>(item.GetProperty("type").GetString() ?? "market", ignoreCase: true),
            Status = ParseOrderStatus(item.GetProperty("status").GetString() ?? ""),
            CreatedAt = DateTime.Parse(item.GetProperty("created_at").GetString() ?? "")
        };

        if (item.TryGetProperty("limit_price", out var limitEl) && limitEl.ValueKind != JsonValueKind.Null)
            order.LimitPrice = ParseDouble(limitEl);

        if (item.TryGetProperty("filled_avg_price", out var fillEl) && fillEl.ValueKind != JsonValueKind.Null)
        {
            order.FillPrice = ParseDouble(fillEl);
            order.FilledAt = DateTime.Now;
        }

        if (item.TryGetProperty("filled_at", out var filledAtEl) && filledAtEl.ValueKind != JsonValueKind.Null)
            order.FilledAt = DateTime.Parse(filledAtEl.GetString() ?? "");

        return order;
    }

    private static int ParseInt(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return int.Parse(element.GetString() ?? "0");
        return element.GetInt32();
    }

    private static OrderStatus ParseOrderStatus(string status) => status.ToLower() switch
    {
        "new" or "accepted" or "pending_new" => OrderStatus.Pending,
        "filled" => OrderStatus.Filled,
        "cancelled" or "canceled" => OrderStatus.Cancelled,
        "rejected" => OrderStatus.Rejected,
        _ => OrderStatus.Pending
    };
}
