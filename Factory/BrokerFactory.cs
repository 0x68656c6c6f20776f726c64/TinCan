using TinCan.Interfaces;
using TinCan.Models;
using TinCan.Services;

namespace TinCan.Factory;

public static class BrokerFactory
{
    public static IBrokerService Create(Settings settings, IMarketDataProviderService marketData, string projectDir)
    {
        var provider = settings.Providers?.Broker ?? "paper";
        var brokerConfig = settings.Broker ?? new BrokerConfig();

        return provider.ToLowerInvariant() switch
        {
            "paper" => new PaperBrokerService(marketData, projectDir, brokerConfig.Paper?.InitialCash ?? 10000.00),
            "alpaca" => new AlpacaBrokerService(
                brokerConfig.Alpaca?.ApiKey ?? "",
                brokerConfig.Alpaca?.SecretKey ?? "",
                brokerConfig.Alpaca?.BaseUrl ?? "https://paper-api.alpaca.markets"),
            _ => throw new InvalidOperationException($"Unknown broker provider: {provider}")
        };
    }
}
