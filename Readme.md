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

* **Market Data Layer** — Provides real-time and historical data (Finnhub)
* **Signal Engine** — Generates trading signals (BUY / SELL / HOLD)
* **Agent Layer (OpenClaw)** — Makes higher-level decisions using signals + context
* **Execution Layer** — Sends orders to broker APIs (Alpaca)
* **Risk Management** — Handles position sizing, stop-loss, and safeguards

---

## 🚀 Getting Started

### 1. Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
* [Finnhub API key](https://finnhub.io/) (free tier available)

### 2. Clone the Project

```bash
git clone https://github.com/0x68656c6c6f20776f726c64/TinCan.git
cd TinCan
```

### 3. Configure API Keys

Copy the example config and add your API key:

```bash
cp settings.example.json settings.json
```

Edit `settings.json` with your Finnhub API key:

```json
{
    "providers": {
        "finnhub": {
            "api_key": "YOUR_FINNHUB_API_KEY",
            "timeout": 5,
            "enabled": true
        }
    },
    "scheduler": {
        "interval_minutes": 5
    }
}
```

> ⚠️ `settings.json` is gitignored — your API keys will never be committed.

### 4. Configure Stocks to Track

Edit `stock_bot/stock_lookup.json`:

```json
{
    "stocks": {
        "AAPL": { "enabled": true },
        "GOOGL": { "enabled": true },
        "U": { "enabled": true, "output": "unity_stock.json" }
    }
}
```

### 5. Run the Project

```bash
dotnet run
```

The app will:
- Fetch stock prices on the configured interval (default: 5 minutes)
- Store results in `stock_bot/results/`

### 6. Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/TinCan.Tests.Unit

# Integration tests only (requires API key in settings.json)
dotnet test tests/TinCan.Tests.Integration
```

---

## ⚙️ Configuration

| File | Description | Committed? |
|------|-------------|------------|
| `settings.json` | API keys and app settings | ❌ No |
| `settings.example.json` | Template for settings.json | ✅ Yes |
| `stock_bot/stock_lookup.json` | Stocks to track | ✅ Yes |
| `stock_bot/stock_bot/settings.json` | Legacy Python config | ❌ No |

---

## 📁 Project Structure

```
TinCan/
├── Program.cs              # Entry point
├── Scheduler.cs            # Main loop & scheduling
├── Models/                # Data models
├── Services/              # Business logic
│   ├── FinnhubService.cs  # Finnhub API integration
│   └── StockFileService.cs # File operations
├── tests/                 # Test projects
│   ├── TinCan.Tests.Unit/
│   └── TinCan.Tests.Integration/
└── stock_bot/            # Data storage
    ├── results/           # Price data output
    └── stock_lookup.json # Stock configuration
```

---

## 🧪 Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/TinCan.Tests.Unit

# Integration tests only
dotnet test tests/TinCan.Tests.Integration
```

Integration tests require a valid Finnhub API key in `settings.json`.

---

## 🧩 Roadmap

* [x] Basic Finnhub market data provider
* [x] Stock file storage
* [x] Unit & integration tests
* [ ] Basic signal engine (MA / RSI)
* [ ] Alpaca broker integration
* [ ] Paper trading
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
