using TinCan.Models;
using TinCan.Services;

namespace TinCan.Factory;

public static class BrokerFactory
{
    public static IBrokerService Create(Settings settings)
    {
        var provider = settings.Providers?.Broker ?? "alpaca";
        var brokerConfig = settings.Broker ?? new BrokerConfig();

        return provider.ToLowerInvariant() switch
        {
            "alpaca" => new AlpacaBrokerService(
                brokerConfig.Alpaca?.ApiKey ?? "",
                brokerConfig.Alpaca?.SecretKey ?? "",
                brokerConfig.Alpaca?.BaseUrl ?? "https://paper-api.alpaca.markets"),
            _ => throw new InvalidOperationException($"Unknown broker provider: {provider}")
        };
    }
}
