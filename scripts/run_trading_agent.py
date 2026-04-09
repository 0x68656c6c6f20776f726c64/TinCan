#!/usr/bin/env python3
"""Non-interactive TradingAgents runner for TinCan CLI.

Usage:
    python run_trading_agent.py <symbol> <date> <analysts> <depth> <llm> <results_path>
"""

import sys
import os
from datetime import datetime

# Add TradingAgents to path
tradingagents_path = os.environ.get("TA_TRADINGAGENTS_PATH")
    if not tradingagents_path or not os.path.exists(tradingagents_path):
        raise RuntimeError("TA_TRADINGAGENTS_PATH environment variable must be set to the TradingAgents directory path")
    sys.path.insert(0, tradingagents_path)

from tradingagents.graph.trading_graph import TradingAgentsGraph
from tradingagents.default_config import DEFAULT_CONFIG

# Load environment variables from .env if present
env_file = os.path.join(tradingagents_path, ".env")
if os.path.exists(env_file):
    with open(env_file) as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, value = line.split("=", 1)
                os.environ.setdefault(key, value)


def main():
    if len(sys.argv) < 6:
        print("[ERROR] Usage: run_trading_agent.py <symbol> <date> <analysts> <depth> <llm> <results_path>")
        sys.exit(1)

    symbol = sys.argv[1]
    date = sys.argv[2]
    analysts_str = sys.argv[3]  # comma-separated
    depth = int(sys.argv[4])
    llm = sys.argv[5]
    results_path = sys.argv[6] if len(sys.argv) > 6 else None

    # Parse analysts
    analysts = [a.strip().lower() for a in analysts_str.split(",")]

    print(f"[INFO] Starting TradingAgent analysis for {symbol}...")
    print(f"[INFO] Date: {date}, Analysts: {analysts}, Depth: {depth}, LLM: {llm}")

    # Map LLM provider to actual model names
    llm_model_map = {
        "minimax": "MiniMax-M2.7",
        "openai": "gpt-4o-mini",
        "google": "gemini-2.0-flash",
        "anthropic": "claude-sonnet-4-20250514",
    }
    model_name = llm_model_map.get(llm.lower(), llm)

    # Create config
    config = DEFAULT_CONFIG.copy()
    config["max_debate_rounds"] = depth
    config["max_risk_discuss_rounds"] = depth
    config["llm_provider"] = llm.lower()
    config["quick_think_llm"] = model_name
    config["deep_think_llm"] = model_name
    config["backend_url"] = "https://api.minimax.io/v1" if llm.lower() == "minimax" else "https://api.openai.com/v1"
    if results_path:
        config["results_dir"] = results_path

    # Initialize graph
    graph = TradingAgentsGraph(
        selected_analysts=analysts,
        config=config,
        debug=False,
    )

    print("[INFO] Running analysis...")

    # Run analysis
    final_state, decision = graph.propagate(symbol, date)

    print(f"[INFO] Analysis completed!")
    print(f"[INFO] Decision: {decision}")

    # Save results
    if results_path:
        from pathlib import Path
        results_dir = Path(results_path) / symbol / date
        results_dir.mkdir(parents=True, exist_ok=True)

        # Save decision with timestamp to avoid duplicates for same symbol/date
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        (results_dir / f"decision_{timestamp}.txt").write_text(str(decision))

        # Save final state summary
        summary = {
            "symbol": symbol,
            "date": date,
            "decision": str(decision),
            "completed_at": datetime.now().isoformat(),
        }
        import json
        (results_dir / "summary.json").write_text(json.dumps(summary, indent=2))
        print(f"[INFO] Results saved to {results_dir}")

    sys.exit(0)


if __name__ == "__main__":
    main()
