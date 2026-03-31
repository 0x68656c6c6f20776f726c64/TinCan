# TinCan 🚀

TinCan is an experimental automation platform designed to integrate **agent-based decision systems (OpenClaw)** with **real-time data sources and execution layers**.

The goal of this project is to provide a flexible foundation for building:

* 🤖 Automated trading bots
* 📊 Real-time data processing pipelines
* ⚙️ Agent-driven decision systems

---

## 🧠 Architecture Overview

```text
Market Data → Signal Engine → Sandbox Simulation → Execution Layer → Risk Control
     ↑              ↓               ↓                ↓               ↓
  Finnhub      OpenClaw            E2B          Broker API     Position Mgmt
```

## 📦 As a CLI Tool

### Install from NuGet

TinCan-CLI is published as a NuGet tool package. You can install it directly from NuGet:

```bash
dotnet tool install -g TinCan-CLI
```

Once installed, use the `tincan` command from anywhere:

```bash
tincan --help
```

### Install from Local Package

If you have the package locally:

```bash
dotnet tool install -g --add-source /tmp/tincan-packages/ TinCan-CLI
```

### Update Package

To update to the latest version:

```bash
dotnet tool update -g TinCan-CLI
```

### Uninstall

To uninstall:

```bash
dotnet tool uninstall -g TinCan-CLI
```

---

## 🛠️ CLI Commands

### `tincan fetch [--interval <minutes>] [--settings <path>]`
Runs the scheduler loop — fetches prices on the configured interval. Same as the original `dotnet run` behavior.
```bash
tincan fetch --interval 5
```

### `tincan price <symbol> [--json]`
Fetches and prints the current price for a single symbol.
```bash
tincan price U
tincan price AAPL --json
```

### `tincan backfill <symbol> --from <YYYY-MM-DD> --to <YYYY-MM-DD>`
Fetches historical OHLCV data and replaces the result file.
```bash
tincan backfill U --from 2024-01-01 --to 2024-12-31
```

### `tincan context <symbol> [--json]`
Loads and displays the current `MarketContext` for a symbol (result file → structured data).
```bash
tincan context U --json
```

### `tincan orders [--open] [--symbol <symbol>] [--provider <provider>]`
Lists orders from the broker. **Stub** — requires Story #13 (Execution Layer).

### `tincan order <orderId> [--provider <provider>]`
Gets details of a specific order. **Stub** — requires Story #13.

### `tincan positions [--provider <provider>]`
Views current positions from the broker. **Stub** — requires Story #13.

### `tincan cancel <orderId> [--provider <provider>]`
Cancels an open order. **Stub** — requires Story #13.

---

## 🚀 Getting Started

### Option 1: Use Published NuGet Package (Recommended)

```bash
# Install the tool
dotnet tool install -g TinCan-CLI

# Configure your API keys
cp settings.example.json settings.json
# Edit settings.json with your Finnhub and Alpaca API keys

# Run commands
tincan fetch --interval 5
tincan price AAPL
```

### Option 2: Development Mode

If you want to contribute or modify TinCan:

```bash
# Clone the repository
git clone https://github.com/0x68656c6c6f20776f726c64/TinCan-CLI.git
cd TinCan-CLI

# Restore and build
dotnet restore
dotnet build

# Configure
cp settings.example.json settings.json
# Edit settings.json with your API keys

# Run
dotnet run -- fetch

# Or pack and install locally
dotnet pack -c Release
dotnet tool install -g --add-source /tmp/tincan-packages/ .
```

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* API keys:
  * [Finnhub](https://finnhub.io) — free tier available
  * [Alpaca](https://alpaca.markets) — for broker integration

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 📁 Project Structure

```
TinCanCLI/
├── Program.cs                  # CLI entry point
├── Scheduler.cs                # Main scheduling loop
├── Commands/                   # CLI command handlers
│   ├── BackfillCommand.cs
│   ├── BalanceCommand.cs
│   ├── BuyCommand.cs
│   ├── CancelCommand.cs
│   ├── ContextCommand.cs
│   ├── FetchCommand.cs
│   ├── OrderCommand.cs
│   ├── OrdersCommand.cs
│   ├── PositionsCommand.cs
│   ├── PriceCommand.cs
│   └── SellCommand.cs
├── Factory/
│   └── BrokerFactory.cs       # Broker service factory
├── Infrastructure/
│   ├── MarketDataProviderFactory.cs  # Market data provider factory
│   ├── ProviderResolver.cs     # Provider resolution (flag > env > config)
│   └── SettingsLoader.cs      # Shared settings loading
├── Interfaces/
│   ├── IBrokerService.cs             # Broker abstraction
│   ├── IMarketDataProviderService.cs  # Market data abstraction
│   └── IStrategy.cs                   # Strategy interface
├── Models/
│   ├── MarketContext.cs       # Market data context for strategies
│   ├── OpenClawResponse.cs    # OpenClaw agent response model
│   ├── Order.cs               # Order, BrokerBalance, OrderResult
│   ├── OrderEnums.cs          # OrderSide, OrderType, OrderStatus
│   ├── Position.cs            # Position (symbol, qty, avg cost, p&l)
│   ├── Settings.cs            # Configuration model
│   ├── Signal.cs              # Trading signal (Buy/Sell/Hold)
│   ├── StockLookup.cs         # Stock tracking config
│   └── StockPrice.cs          # Price data model
├── Services/
│   ├── AlpacaBrokerService.cs # Alpaca API integration
│   ├── FinnhubService.cs      # Finnhub API integration
│   ├── OpenClawService.cs     # OpenClaw agent CLI integration
│   └── StockFileService.cs    # File-based stock data (read/write)
├── Strategies/
│   ├── OpenClawStrategy.cs    # OpenClaw agent-driven strategy
│   └── StrategyBase.cs        # Abstract base class
├── tests/
│   ├── TinCan.Tests.Unit/
│   └── TinCan.Tests.Integration/
└── stock_bot/
    ├── settings.example.json
    └── stock_lookup.json
```

---

## 🗺️ Roadmap

- [x] Market data layer (Finnhub)
- [x] Scheduler with configurable interval
- [x] Unit & integration tests
- [x] Signal Engine framework (IStrategy, StrategyBase)
- [x] OpenClaw-powered strategy
- [x] CLI app with command dispatch
- [x] LoadMarketContext (Story #9)
- [ ] Execution Layer - Broker Abstraction + Paper Trading (Story #13)
- [ ] Risk management module
- [ ] Backtesting framework

---

## ⚠️ Disclaimer

This project is for **educational and experimental purposes only**.
Do not use with real funds without proper testing and risk management.

---

## 📄 License

MIT License
