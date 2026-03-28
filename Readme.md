# TinCan 🚀

TinCan is an experimental automation platform designed to integrate **agent-based decision systems (OpenClaw)** with **real-time data sources and execution layers**.

The goal of this project is to provide a flexible foundation for building:

* 🤖 Automated trading bots
* 📊 Real-time data processing pipelines
* ⚙️ Agent-driven decision systems

---

## 🧠 Architecture Overview

TinCan is designed as a **safe, agent-driven trading system** where all AI-generated strategies are **validated through sandbox simulation before execution**.

```text
Market Data → Signal Engine → Sandbox Simulation → Execution Layer → Risk Control
     ↑              ↓               ↓                ↓               ↓
  Finnhub      OpenClaw            E2B          Broker API     Position Mgmt
```

---

## 🔍 Core Concepts

### 1. Market Data Layer

* Source of truth for price data
* Powered by **Finnhub**
* Provides real-time and historical data for strategy evaluation

---

### 2. Signal Engine (AI-Driven)

* Strategies are defined as **text inputs**
* OpenClaw converts text into executable strategy logic
* Outputs standardized `Signal` objects (Buy / Sell / Hold)

👉 No hardcoded strategies — fully dynamic and extensible

---

### 3. Sandbox Simulation (Critical Safety Layer)

* All AI-generated strategies are executed in a secure sandbox using E2B
* Runs backtesting and forward simulation using historical data
* Prevents unsafe or unverified strategies from reaching live trading

#### Simulation Responsibilities:

* Execute strategy logic deterministically
* Simulate trades over time
* Generate performance metrics:

  * Profit & Loss (PnL)
  * Max Drawdown
  * Win Rate
  * Trade Count
  * Equity Curve

👉 This is the **core validation gate** before any real trade

---

### 4. Execution Layer

* Converts validated `TradeSignal` into real trades
* Integrates with broker APIs (e.g. Alpaca)
* Handles:

  * Order placement
  * Order status tracking
  * Execution feedback

---

### 5. Risk Control Layer

* Final safeguard before capital is deployed
* Enforces:

  * Position sizing limits
  * Max loss per trade
  * Exposure limits
  * Kill switch conditions

👉 Risk logic is **separate from AI strategy logic**

---

## 🔄 End-to-End Workflow

1. User submits strategy (natural language)
2. OpenClaw generates executable strategy code
3. Strategy runs inside E2B sandbox
4. Simulation engine produces performance metrics
5. User reviews results and adjusts risk parameters
6. Approved strategy is deployed to execution layer
7. Risk control enforces safety in live trading

---

## ⚠️ Safety Principles

* AI-generated code is **never executed directly in production**
* All strategies must pass sandbox simulation
* Execution layer is isolated from AI logic
* Risk management overrides all signals

---

## 🧩 Future Enhancements

* Strategy versioning & comparison
* Multi-strategy portfolio orchestration
* Live vs simulated performance tracking
* Advanced risk models (VaR, volatility targeting)
* Distributed backtesting engine

---

## 🚀 Getting Started

### 1. Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* API keys:
  * [Finnhub](https://finnhub.io) — free tier available
  * [Alpaca](https://alpaca.markets) — for paper trading (optional)

### 2. Clone the repo

```bash
git clone https://github.com/0x68656c6c6f20776f726c64/TinCan.git
cd TinCan
```

### 3. Configure settings

Copy the example settings and add your API key:

```bash
# Copy settings file
cp stock_bot/settings.example.json stock_bot/settings.json

# Edit with your API key
# Linux/macOS:
nano stock_bot/settings.json

# Windows:
notepad stock_bot/settings.json
```

Your `stock_bot/settings.json` should look like:

```json
{
    "finnhub": {
        "api_key": "YOUR_FINNHUB_API_KEY",
        "timeout": 5
    },
    "alpaca": {
        "api_key": "YOUR_ALPACA_API_KEY",
        "api_secret": "YOUR_ALPACA_SECRET",
        "base_url": "https://paper-api.alpaca.markets"
    }
}
```

> ⚠️ **Note:** `stock_bot/settings.json` is gitignored — your API keys stay local.

### 4. Add stocks to track

Edit `stock_bot/stock_lookup.json`:

```json
{
    "stocks": {
        "U": {
            "enabled": true,
            "name": "Unity Software",
            "output": "unity_stock.json"
        },
        "AAPL": {
            "enabled": true,
            "name": "Apple",
            "output": "aapl_stock.json"
        }
    }
}
```

### 5. Run the app

```bash
# Restore dependencies
dotnet restore

# Run the app (fetches every 5 minutes)
dotnet run

# Or run once and exit
dotnet run -- --once
```

---

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Unit tests only (no API key needed)
dotnet test tests/TinCan.Tests.Unit/

# Integration tests (requires API key)
dotnet test tests/TinCan.Tests.Integration/

# Or set API key via environment variable
FINNHUB_API_KEY="your_key" dotnet test
```

---

## ⚙️ Project Structure

```
TinCan/
├── Program.cs              # Entry point
├── Scheduler.cs            # Main loop (runs every X minutes)
├── Models/
│   ├── Settings.cs         # Configuration model
│   ├── StockLookup.cs     # Stock tracking config
│   ├── StockPrice.cs      # Price data model
│   ├── Signal.cs          # Trading signal model (Buy/Sell/Hold)
│   └── MarketContext.cs   # Market data context for strategies
├── Strategies/
│   ├── IStrategy.cs       # Strategy interface
│   ├── StrategyBase.cs    # Abstract base class with helper methods
│   ├── OpenClawStrategy.cs     # Base strategy that calls OpenClaw agent
│   └── OpenClawSimpleStrategy.cs  # Simple child strategy
├── Services/
│   ├── FinnhubService.cs  # Fetches stock prices
│   ├── StockFileService.cs# Reads/writes stock data
│   └── OpenClawService.cs # Calls OpenClaw agent for trading signals
├── tests/
│   ├── TinCan.Tests.Unit/        # Unit tests (mocked)
│   └── TinCan.Tests.Integration/ # Integration tests (real API)
└── stock_bot/
    ├── settings.json      # API keys (gitignored)
    ├── settings.example.json
    └── stock_lookup.json  # Stocks to track
```

---

## 🧩 Roadmap

* [x] Market data layer (Finnhub)
* [x] Scheduler with configurable interval
* [x] Unit & integration tests
* [x] Signal Engine framework (IStrategy, StrategyBase)
* [x] OpenClaw-powered strategy
* [ ] Basic signal engine (MA / RSI)
* [ ] Paper trading integration (Alpaca)
* [ ] Risk management module
* [ ] Backtesting framework

---

## ⚠️ Disclaimer

This project is for **educational and experimental purposes only**.
Do not use with real funds without proper testing and risk management.

---

## 📄 License

MIT License
