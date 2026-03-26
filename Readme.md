# TinCan 🚀

TinCan is an experimental automation platform designed to integrate **agent-based decision systems (OpenClaw)** with **real-time data sources and execution layers**.

The goal of this project is to provide a flexible foundation for building:

* 🤖 Automated trading bots
* 📊 Real-time data processing pipelines
* ⚙️ Agent-driven decision systems

---

## 🧠 Architecture Overview

```
Market Data → Signal Engine → Decision Agent → Execution Layer → Risk Control
     ↑              ↓               ↓                ↓               ↓
  Finnhub      Strategy Logic   OpenClaw        Broker API     Position Mgmt
```

### Core Components

* **Market Data Layer** — Finnhub for real-time stock prices
* **Signal Engine** — Generates trading signals (BUY / SELL / HOLD)
* **Agent Layer (OpenClaw)** — Makes higher-level decisions using signals + context
* **Execution Layer** — Sends orders to broker APIs (e.g. Alpaca)
* **Risk Management** — Handles position sizing, stop-loss, and safeguards

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
│   └── StockPrice.cs      # Price data model
├── Services/
│   ├── FinnhubService.cs  # Fetches stock prices
│   └── StockFileService.cs# Reads/writes stock data
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
* [ ] Basic signal engine (MA / RSI)
* [ ] Paper trading integration (Alpaca)
* [ ] OpenClaw decision agent
* [ ] Risk management module
* [ ] Backtesting framework

---

## ⚠️ Disclaimer

This project is for **educational and experimental purposes only**.
Do not use with real funds without proper testing and risk management.

---

## 📄 License

MIT License
