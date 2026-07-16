using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Features;

/// <summary>
/// Series-based technical indicator calculations. Each method returns an array the same
/// length as the input, with leading entries set to <see cref="double.NaN"/> until enough
/// data exists to compute a value. Callers that only need the latest value should read the
/// last array element.
/// </summary>
public static class Indicators
{
    public static double[] Sma(IReadOnlyList<double> values, int period)
    {
        var result = new double[values.Count];
        Array.Fill(result, double.NaN);

        double sum = 0;
        for (int i = 0; i < values.Count; i++)
        {
            sum += values[i];
            if (i >= period) sum -= values[i - period];
            if (i >= period - 1) result[i] = sum / period;
        }

        return result;
    }

    public static double[] Ema(IReadOnlyList<double> values, int period)
    {
        var result = new double[values.Count];
        Array.Fill(result, double.NaN);
        if (values.Count < period) return result;

        double multiplier = 2.0 / (period + 1);

        // Seed with the SMA of the first `period` values.
        double seed = 0;
        for (int i = 0; i < period; i++) seed += values[i];
        seed /= period;
        result[period - 1] = seed;

        for (int i = period; i < values.Count; i++)
        {
            result[i] = (values[i] - result[i - 1]) * multiplier + result[i - 1];
        }

        return result;
    }

    /// <summary>Wilder's RSI.</summary>
    public static double[] Rsi(IReadOnlyList<double> values, int period)
    {
        var result = new double[values.Count];
        Array.Fill(result, double.NaN);
        if (values.Count <= period) return result;

        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            double change = values[i] - values[i - 1];
            if (change > 0) avgGain += change;
            else avgLoss -= change;
        }
        avgGain /= period;
        avgLoss /= period;
        result[period] = RsiFromAverages(avgGain, avgLoss);

        for (int i = period + 1; i < values.Count; i++)
        {
            double change = values[i] - values[i - 1];
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;
            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
            result[i] = RsiFromAverages(avgGain, avgLoss);
        }

        return result;
    }

    private static double RsiFromAverages(double avgGain, double avgLoss)
    {
        if (avgLoss == 0) return 100;
        double rs = avgGain / avgLoss;
        return 100 - 100 / (1 + rs);
    }

    /// <summary>Wilder's ATR (average true range).</summary>
    public static double[] Atr(IReadOnlyList<Candle> candles, int period)
    {
        var result = new double[candles.Count];
        Array.Fill(result, double.NaN);
        if (candles.Count <= period) return result;

        var trueRanges = new double[candles.Count];
        for (int i = 0; i < candles.Count; i++)
        {
            if (i == 0)
            {
                trueRanges[i] = (double)(candles[i].High - candles[i].Low);
                continue;
            }

            double high = (double)candles[i].High;
            double low = (double)candles[i].Low;
            double prevClose = (double)candles[i - 1].Close;
            trueRanges[i] = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
        }

        double atr = 0;
        for (int i = 1; i <= period; i++) atr += trueRanges[i];
        atr /= period;
        result[period] = atr;

        for (int i = period + 1; i < candles.Count; i++)
        {
            atr = (atr * (period - 1) + trueRanges[i]) / period;
            result[i] = atr;
        }

        return result;
    }

    public static (double[] MacdLine, double[] SignalLine, double[] Histogram) Macd(
        IReadOnlyList<double> values, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var fastEma = Ema(values, fastPeriod);
        var slowEma = Ema(values, slowPeriod);

        var macdLine = new double[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            macdLine[i] = double.IsNaN(fastEma[i]) || double.IsNaN(slowEma[i]) ? double.NaN : fastEma[i] - slowEma[i];
        }

        // Signal line is the EMA of the MACD line, computed only over the valid (non-NaN) tail.
        int firstValid = Array.FindIndex(macdLine, v => !double.IsNaN(v));
        var signalLine = new double[values.Count];
        Array.Fill(signalLine, double.NaN);

        if (firstValid >= 0 && values.Count - firstValid >= signalPeriod)
        {
            var macdTail = macdLine.Skip(firstValid).ToArray();
            var signalTail = Ema(macdTail, signalPeriod);
            for (int i = 0; i < signalTail.Length; i++) signalLine[firstValid + i] = signalTail[i];
        }

        var histogram = new double[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            histogram[i] = double.IsNaN(macdLine[i]) || double.IsNaN(signalLine[i]) ? double.NaN : macdLine[i] - signalLine[i];
        }

        return (macdLine, signalLine, histogram);
    }

    /// <summary>Bollinger band width, normalized as (upper - lower) / middle.</summary>
    public static double[] BollingerWidth(IReadOnlyList<double> values, int period = 20, double stdevMultiplier = 2.0)
    {
        var sma = Sma(values, period);
        var result = new double[values.Count];
        Array.Fill(result, double.NaN);

        for (int i = period - 1; i < values.Count; i++)
        {
            double mean = sma[i];
            double sumSq = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                double diff = values[j] - mean;
                sumSq += diff * diff;
            }
            double stdev = Math.Sqrt(sumSq / period);
            double upper = mean + stdevMultiplier * stdev;
            double lower = mean - stdevMultiplier * stdev;
            result[i] = mean == 0 ? 0 : (upper - lower) / mean;
        }

        return result;
    }

    /// <summary>Rolling standard deviation of simple period-over-period returns.</summary>
    public static double[] StdevOfReturns(IReadOnlyList<double> values, int period)
    {
        var returns = new double[values.Count];
        Array.Fill(returns, double.NaN);
        for (int i = 1; i < values.Count; i++)
        {
            returns[i] = values[i - 1] == 0 ? 0 : (values[i] - values[i - 1]) / values[i - 1];
        }

        var result = new double[values.Count];
        Array.Fill(result, double.NaN);
        for (int i = period; i < values.Count; i++)
        {
            double mean = 0;
            for (int j = i - period + 1; j <= i; j++) mean += returns[j];
            mean /= period;

            double sumSq = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                double diff = returns[j] - mean;
                sumSq += diff * diff;
            }
            result[i] = Math.Sqrt(sumSq / period);
        }

        return result;
    }
}
