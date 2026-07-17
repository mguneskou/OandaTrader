using System.Collections.Concurrent;

namespace OandaTrader.Infrastructure.Backtesting;

/// <summary>
/// Guards against two backtests for the same instrument running concurrently. A backtest job
/// outlives the HTTP request that started it (see BacktestController), so nothing else stops
/// a double-click or a second browser tab from kicking off an overlapping run - which then
/// races the first on the same SQLite writes (Candles upsert, then delete+insert of that
/// instrument's synthetic Trades).
/// </summary>
public class BacktestJobTracker
{
    private readonly ConcurrentDictionary<string, bool> _running = new();

    public bool TryStart(string instrument) => _running.TryAdd(instrument, true);

    public void Finish(string instrument) => _running.TryRemove(instrument, out _);
}
