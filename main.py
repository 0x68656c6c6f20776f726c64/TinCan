#!/usr/bin/env python3
"""
TinCan - Main execution loop
Runs providers every X minutes to fetch market data and generate signals
"""

import os
import sys
import time
import json
import argparse
import logging
from datetime import datetime

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S"
)
log = logging.getLogger(__name__)

# Directory structure
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROVIDERS_DIR = os.path.join(SCRIPT_DIR, "stock_bot", "providers")
CONFIG_FILE = os.path.join(SCRIPT_DIR, "config.json")


def load_config():
    """Load configuration from config.json"""
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, "r") as f:
            return json.load(f)
    return {}


def get_providers():
    """Dynamically load all provider scripts"""
    providers = []
    if not os.path.exists(PROVIDERS_DIR):
        log.warning(f"Providers directory not found: {PROVIDERS_DIR}")
        return providers

    for filename in sorted(os.listdir(PROVIDERS_DIR)):
        if filename.endswith("_ws.py") or filename.endswith("_provider.py"):
            filepath = os.path.join(PROVIDERS_DIR, filename)
            provider_name = os.path.splitext(filename)[0]
            log.info(f"Loaded provider: {provider_name}")
            providers.append((provider_name, filepath))

    return providers


def run_provider(provider_name, filepath):
    """Execute a single provider script"""
    log.info(f"Running provider: {provider_name}")
    try:
        # Execute the provider module
        namespace = {}
        with open(filepath, "r") as f:
            code = f.read()
        exec(compile(code, filepath, "exec"), namespace)
        if "main" in namespace:
            namespace["main"]()
        else:
            log.warning(f"Provider {provider_name} has no main() function")
    except Exception as e:
        log.error(f"Provider {provider_name} failed: {e}")


def run_all_providers(providers):
    """Run all enabled providers in sequence"""
    for provider_name, filepath in providers:
        run_provider(provider_name, filepath)


def main():
    parser = argparse.ArgumentParser(description="TinCan - Market data collector")
    parser.add_argument(
        "--interval", "-i",
        type=int,
        default=5,
        help="Interval in minutes between each run (default: 5)"
    )
    parser.add_argument(
        "--once",
        action="store_true",
        help="Run once and exit (no loop)"
    )
    parser.add_argument(
        "--provider",
        type=str,
        help="Run a specific provider only"
    )
    args = parser.parse_args()

    config = load_config()
    interval_seconds = (args.interval or config.get("interval", 5)) * 60

    log.info("=" * 50)
    log.info("TinCan - Starting up")
    log.info(f"Interval: {interval_seconds // 60} minute(s)")
    log.info("=" * 50)

    providers = get_providers()

    if not providers:
        log.error("No providers found. Exiting.")
        sys.exit(1)

    if args.provider:
        providers = [(p, f) for p, f in providers if p == args.provider]
        if not providers:
            log.error(f"Provider '{args.provider}' not found.")
            sys.exit(1)

    if args.once:
        log.info("Running once mode...")
        run_all_providers(providers)
        log.info("Done.")
        return

    # Main loop
    while True:
        try:
            log.info(f"Cycle starting at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
            run_all_providers(providers)
            log.info(f"Cycle complete. Sleeping for {interval_seconds // 60} minute(s)...")
            time.sleep(interval_seconds)
        except KeyboardInterrupt:
            log.info("Shutting down...")
            break
        except Exception as e:
            log.error(f"Unexpected error: {e}")
            log.info("Restarting in 60 seconds...")
            time.sleep(60)


if __name__ == "__main__":
    main()
