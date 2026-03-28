using TinCan.Models;

namespace TinCan.Strategies;

public interface IStrategy
{
    string Name { get; }
    Signal Generate(MarketContext context);
}
