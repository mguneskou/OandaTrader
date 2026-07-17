using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Strategies;

namespace OandaTrader.Domain.Backtesting;

/// <summary>
/// Replays a strategy bar-by-bar over historical candles. Used to bootstrap the ML model
/// (see plan's cold-start section): BaselineStrategy runs over historical data to produce a
/// synthetic (features, outcome) dataset before the app ever places a live trade.
///
/// Signals are evaluated independently of each other (a new signal isn't suppressed just
/// because a previous one hasn't resolved yet) — the goal here is generating training
/// samples, not simulating a single real portfolio's equity curve, so overlapping trades
/// are intentional and increase the sample count.
/// </summary>
public static class BacktestEngine
{
    /// <param name="onProgress">Reported as an integer percent (0-100), at most once per
    /// point change - callers driving a UI progress bar can wire this directly.</param>
    public static (List<BacktestTradeResult> Trades, BacktestSummary Summary) Run(
        string instrument, IReadOnlyList<Candle> candles, IStrategy strategy, Action<int>? onProgress = null)
    {
        var results = new List<BacktestTradeResult>();
        var history = candles as List<Candle> ?? candles.ToList();

        int firstIndex = FeatureBuilder.MinimumCandleCount - 1;
        int totalBars = candles.Count - firstIndex;
        int lastReportedPercent = -1;

        for (int i = firstIndex; i < candles.Count; i++)
        {
            var window = history.GetRange(0, i + 1);

            var decision = strategy.Evaluate(new StrategyContext { Instrument = instrument, History = window });
            if (decision is not null)
            {
                var result = SimulateExit(candles, i, decision);
                if (result is not null) results.Add(result);
            }

            if (onProgress is not null && totalBars > 0)
            {
                int percent = (i - firstIndex + 1) * 100 / totalBars;
                if (percent != lastReportedPercent)
                {
                    lastReportedPercent = percent;
                    onProgress(percent);
                }
            }
        }

        return (results, Summarize(results));
    }

    private static BacktestTradeResult? SimulateExit(IReadOnlyList<Candle> candles, int decisionIndex, TradeDecision decision)
    {
        decimal riskDistance = Math.Abs(decision.EntryPrice - decision.StopLoss);
        var entryTime = candles[decisionIndex].TimestampUtc;

        for (int j = decisionIndex + 1; j < candles.Count; j++)
        {
            var candle = candles[j];

            bool hitStop, hitTarget;
            if (decision.Direction == TradeDirection.Long)
            {
                hitStop = candle.Low <= decision.StopLoss;
                hitTarget = candle.High >= decision.TakeProfit;
            }
            else
            {
                hitStop = candle.High >= decision.StopLoss;
                hitTarget = candle.Low <= decision.TakeProfit;
            }

            // If a single candle's range touches both levels we can't know which came first
            // from OHLC alone, so we conservatively assume the stop hit first.
            if (hitStop)
            {
                return BuildResult(decision, decision.StopLoss, candle.TimestampUtc, entryTime, TradeOutcomeResult.Loss, riskDistance);
            }
            if (hitTarget)
            {
                return BuildResult(decision, decision.TakeProfit, candle.TimestampUtc, entryTime, TradeOutcomeResult.Win, riskDistance);
            }
        }

        return null; // ran out of data before either level was hit; can't label this sample
    }

    private static BacktestTradeResult BuildResult(
        TradeDecision decision, decimal exitPrice, DateTime exitTime, DateTime entryTime,
        TradeOutcomeResult outcome, decimal riskDistance)
    {
        decimal pnlInPrice = decision.Direction == TradeDirection.Long
            ? exitPrice - decision.EntryPrice
            : decision.EntryPrice - exitPrice;
        decimal pnlInR = riskDistance == 0 ? 0 : pnlInPrice / riskDistance;

        return new BacktestTradeResult
        {
            Instrument = decision.Instrument,
            Direction = decision.Direction,
            EntryPrice = decision.EntryPrice,
            EntryTimeUtc = entryTime,
            StopLoss = decision.StopLoss,
            TakeProfit = decision.TakeProfit,
            ExitPrice = exitPrice,
            ExitTimeUtc = exitTime,
            Outcome = outcome,
            Features = decision.Features,
            ReasoningText = decision.ReasoningText,
            PnLInR = pnlInR,
        };
    }

    private static BacktestSummary Summarize(IReadOnlyList<BacktestTradeResult> trades)
    {
        int wins = trades.Count(t => t.Outcome == TradeOutcomeResult.Win);
        int losses = trades.Count(t => t.Outcome == TradeOutcomeResult.Loss);
        decimal winRate = trades.Count == 0 ? 0 : (decimal)wins / trades.Count * 100m;
        decimal totalR = trades.Sum(t => t.PnLInR);

        return new BacktestSummary
        {
            TradeCount = trades.Count,
            Wins = wins,
            Losses = losses,
            WinRatePercent = winRate,
            TotalPnLInR = totalR,
        };
    }
}
