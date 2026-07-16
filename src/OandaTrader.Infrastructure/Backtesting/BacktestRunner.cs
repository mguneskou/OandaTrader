using System.Text.Json;
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
    public async Task<BacktestRunResult> RunAsync(
        string instrument, Granularity granularity, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var candles = await candleCache.GetCandlesAsync(instrument, granularity, fromUtc, toUtc, ct);
        var (trades, summary) = BacktestEngine.Run(instrument, candles, new BaselineStrategy());

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
            ResultSummaryJson = JsonSerializer.Serialize(summary),
        };
        db.BacktestRuns.Add(run);

        await db.SaveChangesAsync(ct);

        return new BacktestRunResult { Run = run, Summary = summary };
    }
}
