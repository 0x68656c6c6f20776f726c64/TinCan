using TinCan.Models;

namespace TinCan.Strategies;

public interface IStrategy
{
    string Name { get; }
    Task<Signal> GenerateAsync(MarketContext context);
}
