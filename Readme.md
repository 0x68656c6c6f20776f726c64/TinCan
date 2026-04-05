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
    "finnhub": {
      "api_key": "",
      "timeout": 5,
      "enabled": true
    },
    "broker": "alpaca"
  },
  "scheduler": {
    "interval_minutes": 5
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

The CLI looks for configuration in `settings.json` first, then `stock_bot/settings.json` by default.

Copy the example file and fill in only the keys you need:
```bash
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your Finnhub and optional Alpaca keys
```

---

## 🚀 Getting Started

### Option 1: Use Published NuGet Package (Recommended)

```bash
# Install the tool
dotnet tool install -g TinCan-CLI

# Configure your API keys
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your Finnhub and optional Alpaca API keys

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
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your API keys

# Run
dotnet run -- fetch

# Or pack and install locally
dotnet pack -c Release
dotnet tool install -g --add-source /tmp/tincan-packages/ .
```

### Prerequisites

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
cp stock_bot/settings.example.json stock_bot/settings.json
# Edit stock_bot/settings.json with your Finnhub API key
# Add Alpaca keys too if you want trading commands
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

```text
TinCan/
|-- Program.cs                     # CLI entry point
|-- Scheduler.cs                   # Scheduler loop
|-- Commands/                      # CLI command handlers
|   |-- BackfillCommand.cs
|   |-- BalanceCommand.cs
|   |-- BuyCommand.cs
|   |-- CancelCommand.cs
|   |-- ContextCommand.cs
|   |-- FetchCommand.cs
|   |-- OrderCommand.cs
|   |-- OrdersCommand.cs
|   |-- PositionsCommand.cs
|   |-- PriceCommand.cs
|   `-- SellCommand.cs
|-- Factory/
|   |-- BrokerFactory.cs
|   `-- MarketDataProviderFactory.cs
|-- Infrastructure/
|   |-- ProviderResolver.cs
|   `-- SettingsLoader.cs
|-- Interfaces/
|   |-- IBrokerService.cs
|   `-- IMarketDataProviderService.cs
|-- Models/
|   |-- MarketContext.cs
|   |-- OpenClawResponse.cs
|   |-- Order.cs
|   |-- OrderEnums.cs
|   |-- Position.cs
|   |-- Settings.cs
|   |-- Signal.cs
|   |-- StockLookup.cs
|   `-- StockPrice.cs
|-- Services/
|   |-- AlpacaBrokerService.cs
|   |-- FinnhubService.cs
|   `-- StockFileService.cs
|-- stock_bot/
|   |-- settings.example.json
|   |-- settings.json
|   `-- stock_lookup.json
|-- tests/
|   |-- TinCan.Tests.Integration/
|   `-- TinCan.Tests.Unit/
|-- TinCan.csproj
`-- TinCan.sln
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
