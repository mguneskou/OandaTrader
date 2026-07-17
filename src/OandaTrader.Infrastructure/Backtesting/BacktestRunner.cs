using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Backtesting;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Strategies;
using OandaTrader.Infrastructure.Data;
using BacktestRunEntity = OandaTrader.Infrastructure.Data.Entities.BacktestRun;
using TradeEntity = OandaTrader.Infrastructure.Data.Entities.Trade;

namespace OandaTrader.Infrastructure.Backtesting;

public class BacktestRunResult
{
    public required BacktestRunEntity Run { get; init; }
    public required BacktestSummary Summary { get; init; }
}

/// <summary>Orchestrates the cold-start bootstrap described in the plan: fetch/cache
/// historical candles, replay BaselineStrategy over them via BacktestEngine, and persist
/// both the synthetic trades (StrategySource=Backtest, become ML training samples) and a
/// BacktestRun summary row.</summary>
public class BacktestRunner(AppDbContext db, CandleCacheService candleCache)
{
    /// <param name="onProgress">Stage name ("Fetching" or "Simulating") plus a 0-100 percent
    /// within that stage. Fetching has no granular progress of its own (a handful of REST
    /// pages at most), so it just reports 0 then jumps to the simulate stage.</param>
    public async Task<BacktestRunResult> RunAsync(
        string instrument, Granularity granularity, DateTime fromUtc, DateTime toUtc,
        Action<string, int>? onProgress = null, CancellationToken ct = default)
    {
        onProgress?.Invoke("Fetching", 0);
        var candles = await candleCache.GetCandlesAsync(instrument, granularity, fromUtc, toUtc, ct);

        var (trades, summary) = BacktestEngine.Run(
            instrument, candles, new BaselineStrategy(),
            onProgress is null ? null : percent => onProgress("Simulating", percent));

        // A re-run over an overlapping window replaces that window's synthetic trades rather
        // than stacking a second copy on top — duplicates would both inflate the analytics
        // and overweight the repeated samples during training.
        var superseded = await db.Trades
            .Where(t => t.StrategySource == StrategySource.Backtest
                        && t.Instrument == instrument
                        && t.EntryTimeUtc >= fromUtc
                        && t.EntryTimeUtc < toUtc)
            .ToListAsync(ct);
        if (superseded.Count > 0)
        {
            db.Trades.RemoveRange(superseded);
        }

        var tradeEntities = trades.Select(t => new TradeEntity
        {
            Instrument = t.Instrument,
            Direction = t.Direction,
            EntryPrice = t.EntryPrice,
            EntryTimeUtc = t.EntryTimeUtc,
            StopLoss = t.StopLoss,
            TakeProfit = t.TakeProfit,
            Units = 0, // backtest trades aren't sized against a real account
            ExitPrice = t.ExitPrice,
            ExitTimeUtc = t.ExitTimeUtc,
            PnL = t.PnLInR,
            Outcome = t.Outcome,
            StrategySource = StrategySource.Backtest,
            FeaturesJson = JsonSerializer.Serialize(t.Features),
            ReasoningText = t.ReasoningText,
        }).ToList();

        db.Trades.AddRange(tradeEntities);

        var run = new BacktestRunEntity
        {
            StartDate = fromUtc,
            EndDate = toUtc,
            Instrument = instrument,
            Granularity = granularity,
            // camelCase: this field is read back by the frontend (unlike FeaturesJson, which
            // only ever round-trips through backend code), so it needs to match the API's
            // usual JSON casing rather than System.Text.Json's PascalCase default.
            ResultSummaryJson = JsonSerializer.Serialize(summary, JsonSerializerOptions.Web),
        };
        db.BacktestRuns.Add(run);

        await db.SaveChangesAsync(ct);

        return new BacktestRunResult { Run = run, Summary = summary };
    }
}
