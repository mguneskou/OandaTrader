using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Strategies;

/// <summary>
/// The seed strategy used to bootstrap the backtest (see plan's cold-start section):
/// EMA20/EMA50 crossover, filtered by RSI14 to avoid entering into an already-extended
/// move, with ATR-based stop-loss/take-profit. Simple and explainable by design — this is
/// what generates the synthetic trade history the first ML model trains on.
/// </summary>
public class BaselineStrategy : IStrategy
{
    public string Name => "Baseline";

    private const int EmaFastPeriod = 20;
    private const int EmaSlowPeriod = 50;
    private const int RsiPeriod = 14;
    private const int AtrPeriod = 14;
    private const double RsiOverbought = 70;
    private const double RsiOversold = 30;
    private const decimal AtrStopMultiplier = 1.5m;
    private const decimal AtrTargetMultiplier = 2.5m;

    public TradeDecision? Evaluate(StrategyContext context)
    {
        var candles = context.History;
        if (candles.Count < FeatureBuilder.MinimumCandleCount)
        {
            return null;
        }

        var closes = candles.Select(c => (double)c.Close).ToList();
        int last = closes.Count - 1;

        var emaFast = Indicators.Ema(closes, EmaFastPeriod);
        var emaSlow = Indicators.Ema(closes, EmaSlowPeriod);
        var rsi = Indicators.Rsi(closes, RsiPeriod);
        var atr = Indicators.Atr(candles, AtrPeriod);

        double prevFast = emaFast[last - 1], prevSlow = emaSlow[last - 1];
        double curFast = emaFast[last], curSlow = emaSlow[last];
        double currentRsi = rsi[last];
        double atrValue = atr[last];

        if (double.IsNaN(prevFast) || double.IsNaN(prevSlow) || double.IsNaN(currentRsi) || double.IsNaN(atrValue) || atrValue <= 0)
        {
            return null;
        }

        bool bullishCross = prevFast <= prevSlow && curFast > curSlow;
        bool bearishCross = prevFast >= prevSlow && curFast < curSlow;

        TradeDirection direction;
        string reason;

        if (bullishCross && currentRsi < RsiOverbought)
        {
            direction = TradeDirection.Long;
            reason = $"EMA{EmaFastPeriod} crossed above EMA{EmaSlowPeriod}; RSI{RsiPeriod}={currentRsi:F1} (< {RsiOverbought} overbought filter).";
        }
        else if (bearishCross && currentRsi > RsiOversold)
        {
            direction = TradeDirection.Short;
            reason = $"EMA{EmaFastPeriod} crossed below EMA{EmaSlowPeriod}; RSI{RsiPeriod}={currentRsi:F1} (> {RsiOversold} oversold filter).";
        }
        else
        {
            return null;
        }

        decimal entry = candles[last].Close;
        decimal atrDecimal = (decimal)atrValue;
        decimal stopLoss = direction == TradeDirection.Long
            ? entry - atrDecimal * AtrStopMultiplier
            : entry + atrDecimal * AtrStopMultiplier;
        decimal takeProfit = direction == TradeDirection.Long
            ? entry + atrDecimal * AtrTargetMultiplier
            : entry - atrDecimal * AtrTargetMultiplier;

        return new TradeDecision
        {
            Instrument = context.Instrument,
            Direction = direction,
            EntryPrice = entry,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            ReasoningText = reason,
            Features = FeatureBuilder.Build(candles),
        };
    }
}
