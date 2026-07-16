# OandaTrader

A web-based auto-trading bot for an **Oanda practice (demo) account**. It trades a basket of
FX majors on its own and learns from its own results: every trade's feature snapshot and
outcome is recorded, and an ML.NET model is periodically refit on that history and only
promoted if it beats the model currently in use.

> This targets Oanda's **practice** environment. Don't point it at a live account.

## Requirements

- Visual Studio 2022 with the ASP.NET/web workload (.NET 9)
- Node.js 20+
- An Oanda **practice** account: an API token and an account ID (e.g. `101-004-XXXXXXX-001`)

## Setup

1. Open `OandaTrader.sln` in VS2022 (`OandaTrader.Api` is the startup project).
2. Right-click **OandaTrader.Api** → **Manage User Secrets**, and paste:

   ```json
   {
     "Oanda": {
       "ApiToken": "<your practice API token>",
       "AccountId": "<your practice account id>"
     }
   }
   ```

   Secrets live outside the repo and are never committed.
3. Press **F5**. The API starts and launches the Vite dev server, then opens the dashboard.
   Breakpoints work in the API and the trading engine; the React app debugs in browser devtools.

## First run

The engine won't place a trade until a model exists — that's deliberate, since an unscored
strategy would be trading blind.

1. **Model → Run backtest** for each instrument. This replays the baseline strategy over
   historical candles to build a labelled training set.
2. **Model → Train model now.** Check the hold-out AUC and that it was promoted.
3. **Dashboard → Start engine.**

Tune anything on **Settings** (risk %, timeframe, confidence threshold, circuit breakers);
the engine picks changes up on its next tick without a restart.

## How it works

```
OandaTrader.Domain          strategies, indicators, risk math, backtest engine (no I/O)
OandaTrader.Infrastructure  Oanda REST + price streaming, EF Core/SQLite, ML.NET
OandaTrader.Api             hosted trading engine, REST API, SignalR hub  ← startup project
OandaTrader.Web             React + TypeScript dashboard (Vite)
```

**The loop.** Every 30s the engine reads the account, reconciles trades Oanda has closed,
checks the circuit breakers, and — per enabled instrument — evaluates the strategy on the
latest closed candle. A signal is sized by fixed-fractional risk and sent as a market order
with its stop-loss and take-profit attached.

**The learning.** `BaselineStrategy` (EMA crossover + RSI filter, ATR-based stop/target)
proposes candidates. The ML model scores each one's win probability and drops anything below
your confidence threshold — it's trained on exactly the population of signals it gates, so
training and live serving stay symmetric. After every *N* closed trades it refits on
everything accumulated (backtest + live), evaluates on a chronological hold-out slice, and
keeps the old model if the new one isn't better.

**Safety.** Max daily loss %, max concurrent positions, and max trades/day each auto-pause
trading. A pause never clears itself — you resume it from the dashboard. Open positions keep
their stop/target on Oanda's side while paused.

**Prices.** Streamed live from Oanda for the dashboard and P&L; strategy signals use the REST
candles endpoint, so bar closes come from Oanda rather than being re-aggregated locally.

## Data

SQLite at `src/OandaTrader.Api/App_Data/oandatrader.db`, created/migrated on startup; trained
models sit beside it under `App_Data/Models`. Both are gitignored — delete them to start over.

## Tests

```
dotnet test
```

Covers the indicators, position sizing, circuit-breaker triggers, strategy signal/gating
behaviour, and the backtest engine.
