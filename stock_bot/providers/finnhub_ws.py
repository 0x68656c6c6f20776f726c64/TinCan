#!/usr/bin/env python3
"""
Finnhub stock tracker - single run
Reads from stock_lookup.json and updates stock JSON files
"""

import json
import os
import urllib.request
from datetime import datetime

# Finnhub API Key
API_KEY = os.environ.get("FINNHUB_API_KEY", "d6u58qpr01qp1k9ba1sgd6u58qpr01qp1k9ba1t0")

# Directories
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
STOCK_BOT_DIR = os.path.dirname(SCRIPT_DIR)
LOOKUP_FILE = os.path.join(STOCK_BOT_DIR, "stock_lookup.json")
RESULTS_DIR = os.path.join(STOCK_BOT_DIR, "results")

def load_lookup():
    try:
        with open(LOOKUP_FILE, "r") as f:
            return json.load(f)
    except:
        return {}

def get_enabled_stocks(lookup):
    stocks = lookup.get("stocks", {})
    return {symbol: info for symbol, info in stocks.items() if info.get("enabled", False)}

def get_output_file(symbol, stocks_info):
    if symbol in stocks_info and stocks_info[symbol].get("output"):
        return stocks_info[symbol]["output"]
    return f"{symbol.lower()}_stock.json"

def fetch_price(symbol):
    """Fetch price via REST API"""
    url = f"https://finnhub.io/api/v1/quote?symbol={symbol}&token={API_KEY}"
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            data = json.loads(response.read())
            if data.get('c', 0) > 0:
                return {
                    'price': data['c'],
                    'high': data['h'],
                    'low': data['l'],
                    'timestamp': data['t']
                }
    except Exception as e:
        print(f"Error fetching {symbol}: {e}")
    return None

def update_stock_file(symbol, price, high, low):
    """Update JSON file with new entry"""
    lookup = load_lookup()
    stocks = lookup.get("stocks", {})
    
    if symbol not in stocks:
        return
    
    output_file = get_output_file(symbol, stocks)
    filepath = os.path.join(RESULTS_DIR, output_file)
    
    # Load existing data
    data = []
    if os.path.exists(filepath):
        try:
            with open(filepath, "r") as f:
                data = json.load(f)
        except:
            data = []
    
    # Always add new entry
    print(f"{symbol}: ${price}")
    
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S CT")
    entry = {
        "time": timestamp,
        "price": price,
        "high": high,
        "low": low
    }
    data.append(entry)
    
    with open(filepath, "w") as f:
        json.dump(data, f, indent=2)
    
    print(f"✅ Updated {output_file}: ${price}")

def main():
    lookup = load_lookup()
    stocks = get_enabled_stocks(lookup)
    
    if not stocks:
        print("No stocks to track.")
        return

    print(f"Checking {len(stocks)} stock(s): {', '.join(stocks.keys())}")
    
    for symbol in stocks.keys():
        price_data = fetch_price(symbol)
        if price_data:
            update_stock_file(
                symbol, 
                price_data['price'], 
                price_data['high'], 
                price_data['low']
            )

if __name__ == "__main__":
    main()
