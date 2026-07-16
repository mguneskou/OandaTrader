using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Features;

public static class FeatureBuilder
{
    /// <summary>Minimum candle history required for every indicator (dominated by EMA50) to be non-NaN.</summary>
    public const int MinimumCandleCount = 60;

    /// <summary>Builds the feature vector for the most recently closed candle in <paramref name="history"/>
    /// (oldest first, newest last).</summary>
    public static FeatureVector Build(IReadOnlyList<Candle> history)
    {
        if (history.Count < MinimumCandleCount)
        {
            throw new ArgumentException(
                $"Need at least {MinimumCandleCount} candles to build features, got {history.Count}.", nameof(history));
        }

        var closes = history.Select(c => (double)c.Close).ToList();
        int last = closes.Count - 1;

        var ema20 = Indicators.Ema(closes, 20);
        var ema50 = Indicators.Ema(closes, 50);
        var rsi14 = Indicators.Rsi(closes, 14);
        var atr14 = Indicators.Atr(history, 14);
        var (macdLine, macdSignal, macdHistogram) = Indicators.Macd(closes);
        var bollingerWidth = Indicators.BollingerWidth(closes, 20);
        var stdevReturns = Indicators.StdevOfReturns(closes, 20);

        double lastClose = closes[last];
        var lastTimestamp = history[last].TimestampUtc;

        return new FeatureVector
        {
            Ema20 = ema20[last],
            Ema50 = ema50[last],
            Rsi14 = rsi14[last],
            Atr14 = atr14[last],
            MacdLine = macdLine[last],
            MacdSignal = macdSignal[last],
            MacdHistogram = macdHistogram[last],
            BollingerWidth20 = bollingerWidth[last],
            PriceDistanceFromEma20Pct = ema20[last] == 0 ? 0 : (lastClose - ema20[last]) / ema20[last] * 100.0,
            RecentVolatility20 = stdevReturns[last],
            HourOfDayUtc = lastTimestamp.Hour,
            DayOfWeek = (double)lastTimestamp.DayOfWeek,
        };
    }
}
