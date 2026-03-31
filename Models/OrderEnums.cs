namespace TinCan.Models;

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit
}

public enum OrderStatus
{
    Pending,
    Filled,
    Cancelled,
    Rejected
}
