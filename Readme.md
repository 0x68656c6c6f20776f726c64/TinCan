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

## 🚀 Getting Started (Development)

### 1. Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* API keys:
  * [Finnhub](https://finnhub.io) — free tier available

### 2. Clone and build

```bash
git clone https://github.com/0x68656c6c6f20776f726c64/TinCan-CLI.git
cd TinCan-CLI
dotnet restore
dotnet build
```

### 3. Configure

```bash
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your Finnhub API key
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
│   ├── PositionsCommand.cs
│   └── CancelCommand.cs
├── Infrastructure/
│   ├── SettingsLoader.cs      # Shared settings loading
│   └── ProviderResolver.cs    # Provider resolution (flag > env > config)
├── Models/
│   ├── Settings.cs            # Configuration model
│   ├── StockLookup.cs         # Stock tracking config
│   ├── StockPrice.cs          # Price data model
│   ├── Signal.cs              # Trading signal (Buy/Sell/Hold)
│   ├── MarketContext.cs       # Market data context for strategies
│   └── OpenClawResponse.cs    # OpenClaw agent response model
├── Interfaces/
│   ├── IMarketDataProviderService.cs   # Market data abstraction
│   └── IStrategy.cs                   # Strategy interface
├── Strategies/
│   ├── StrategyBase.cs                # Abstract base class
│   ├── RangeTradingStrategy.cs         # Range trading strategy
│   ├── OpenClawStrategy.cs            # OpenClaw agent-driven strategy
│   └── OpenClawSimpleStrategy.cs       # Simple OpenClaw child strategy
├── Services/
│   ├── FinnhubService.cs       # Finnhub API integration
│   ├── StockFileService.cs     # File-based stock data (read/write)
│   └── OpenClawService.cs      # OpenClaw agent CLI integration
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
