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

TinCan can be installed as a global CLI tool:

```bash
dotnet tool install -g TinCan
```

Once installed, use the `tincan` command from anywhere:

```bash
tincan --help
```

---

## 🛠️ CLI Commands

### `tincan fetch [--interval <minutes>] [--settings <path>]`
Runs the scheduler loop — fetches prices on the configured interval.
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
Loads and displays the current `MarketContext` for a symbol.
```bash
tincan context U --json
```

### `tincan orders [--open] [--symbol <symbol>] [--provider <provider>] [--settings <path>]`
Lists orders from the broker.
```bash
tincan orders --settings stock_bot/settings.json
tincan orders --open --settings stock_bot/settings.json
```

### `tincan order <orderId> [--provider <provider>] [--settings <path>]`
Gets details of a specific order.
```bash
tincan order abc123 --settings stock_bot/settings.json
```

### `tincan buy <symbol> <quantity> [--limit <price>] [--settings <path>]`
Places a buy order.
```bash
tincan buy U 10 --settings stock_bot/settings.json
tincan buy AAPL 5 --limit 150.00 --settings stock_bot/settings.json
```

### `tincan sell <symbol> <quantity> [--limit <price>] [--settings <path>]`
Places a sell order.
```bash
tincan sell U 5 --settings stock_bot/settings.json
tincan sell AAPL 3 --limit 160.00 --settings stock_bot/settings.json
```

### `tincan positions [--provider <provider>] [--settings <path>]`
Views current positions from the broker.
```bash
tincan positions --settings stock_bot/settings.json
```

### `tincan cancel <orderId> [--provider <provider>] [--settings <path>]`
Cancels an open order.
```bash
tincan cancel abc123 --settings stock_bot/settings.json
```

---

## 🔧 Broker Configuration

TinCan supports multiple broker providers:

### Paper Trading (default)
```json
{
  "providers": {
    "broker": "paper"
  },
  "broker": {
    "paper": {
      "initialCash": 10000.00
    }
  }
}
```

### Alpaca
```json
{
  "providers": {
    "broker": "alpaca"
  },
  "broker": {
    "alpaca": {
      "apiKey": "PK...",
      "secretKey": "Sec...",
      "baseUrl": "https://paper-api.alpaca.markets"
    }
  }
}
```

---

## 🚀 Getting Started (Development)

### 1. Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* API keys:
  * [Finnhub](https://finnhub.io) — free tier available for market data
  * [Alpaca](https://alpaca.markets) — optional, for paper/live trading

### 2. Clone and build

```bash
git clone https://github.com/0x68656c6c6f20776f726c64/TinCan-CLI.git
cd TinCan-CLI
dotnet restore
dotnet build
```

### 3. Configure

```bash
cp settings.example.json settings.json
# Edit settings.json with your Finnhub API key
```

For trading, also add Alpaca API keys to `stock_bot/settings.json`:
```bash
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your broker settings
```

### 4. Run

```bash
# Run as CLI
dotnet run -- fetch

# Or install as global tool
dotnet pack -c Release
dotnet tool install -g --add-source /tmp/tincan-packages/ .

# Then use globally
tincan fetch
```

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 📁 Project Structure

```
TinCan/
├── Program.cs                  # CLI entry point
├── Scheduler.cs               # Main loop
├── Commands/                  # CLI command handlers
│   ├── FetchCommand.cs
│   ├── PriceCommand.cs
│   ├── BackfillCommand.cs
│   ├── ContextCommand.cs
│   ├── OrdersCommand.cs
│   ├── OrderCommand.cs
│   ├── BuyCommand.cs
│   ├── SellCommand.cs
│   ├── PositionsCommand.cs
│   └── CancelCommand.cs
├── Infrastructure/
│   ├── SettingsLoader.cs      # Shared settings loading
│   ├── ProviderResolver.cs    # Provider resolution (flag > env > config)
│   └── MarketDataProviderFactory.cs  # Market data factory
├── Factory/
│   └── BrokerFactory.cs       # Broker service factory
├── Models/
│   ├── Settings.cs            # Configuration model
│   ├── StockLookup.cs         # Stock tracking config
│   ├── StockPrice.cs          # Price data model
│   ├── Signal.cs              # Trading signal (Buy/Sell/Hold)
│   ├── MarketContext.cs       # Market data context for strategies
│   ├── Order.cs               # Order, BrokerBalance, ExecutionResult
│   └── OrderEnums.cs          # OrderSide, OrderType, OrderStatus
├── Interfaces/
│   ├── IMarketDataProviderService.cs   # Market data abstraction
│   ├── IBrokerService.cs             # Broker abstraction
│   └── IStrategy.cs                  # Strategy interface
├── Services/
│   ├── FinnhubService.cs       # Finnhub API integration
│   ├── StockFileService.cs     # File-based stock data (read/write)
│   ├── OpenClawService.cs     # OpenClaw agent CLI integration
│   ├── PaperBrokerService.cs   # Paper trading simulation
│   └── AlpacaBrokerService.cs # Alpaca API integration
├── Strategies/
│   ├── StrategyBase.cs                # Abstract base class
│   ├── RangeTradingStrategy.cs         # Range trading strategy
│   ├── OpenClawStrategy.cs            # OpenClaw agent-driven strategy
│   └── OpenClawSimpleStrategy.cs       # Simple OpenClaw child strategy
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
- [x] Execution Layer - Broker Abstraction + Paper Trading (Story #13)
- [ ] Risk management module
- [ ] Backtesting framework

---

## ⚠️ Disclaimer

This project is for **educational and experimental purposes only**.
Do not use with real funds without proper testing and risk management.

---

## 📄 License

MIT License
