using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Tests.TestSupport;

public static class CandleFactory
{
    /// <summary>Generates a synthetic M15 candle series that steps by a fixed amount each bar,
    /// starting at <paramref name="start"/>. A small fixed wick is added above/below each body.</summary>
    public static List<Candle> GenerateTrend(int count, decimal start, decimal stepPerCandle, string instrument = "EUR_USD")
    {
        var candles = new List<Candle>(count);
        var time = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        decimal price = start;

        for (int i = 0; i < count; i++)
        {
            decimal open = price;
            decimal close = price + stepPerCandle;
            decimal high = Math.Max(open, close) + 0.0005m;
            decimal low = Math.Min(open, close) - 0.0005m;

            candles.Add(new Candle
            {
                Instrument = instrument,
                Granularity = Granularity.M15,
                TimestampUtc = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 100,
            });

            price = close;
            time = time.AddMinutes(15);
        }

        return candles;
    }

    /// <summary>Concatenates two trend segments into one continuous series (second segment
    /// continues from the first's ending price).</summary>
    public static List<Candle> GenerateTwoStageTrend(
        int firstCount, decimal firstStep, int secondCount, decimal secondStep,
        decimal start = 1.1000m, string instrument = "EUR_USD")
    {
        var first = GenerateTrend(firstCount, start, firstStep, instrument);
        var second = GenerateTrend(secondCount, first[^1].Close, secondStep, instrument);
        return Concat(first, second);
    }

    /// <summary>A repeating step cycle instead of a single fixed step — used to build a series
    /// that trends net-up/down while still containing enough opposite-direction bars that RSI
    /// stays away from the 0/100 extremes (a smooth monotonic run pins RSI at an extreme as
    /// soon as the lookback window rolls over to all-same-direction moves).</summary>
    public static List<Candle> GenerateChoppyTrend(int count, decimal start, decimal[] stepCycle, string instrument = "EUR_USD")
    {
        var candles = new List<Candle>(count);
        var time = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        decimal price = start;

        for (int i = 0; i < count; i++)
        {
            decimal step = stepCycle[i % stepCycle.Length];
            decimal open = price;
            decimal close = price + step;
            decimal high = Math.Max(open, close) + 0.0005m;
            decimal low = Math.Min(open, close) - 0.0005m;

            candles.Add(new Candle
            {
                Instrument = instrument,
                Granularity = Granularity.M15,
                TimestampUtc = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 100,
            });

            price = close;
            time = time.AddMinutes(15);
        }

        return candles;
    }

    public static List<Candle> GenerateTrendThenChoppy(
        int firstCount, decimal firstStep, int secondCount, decimal[] secondStepCycle,
        decimal start = 1.1000m, string instrument = "EUR_USD")
    {
        var first = GenerateTrend(firstCount, start, firstStep, instrument);
        var second = GenerateChoppyTrend(secondCount, first[^1].Close, secondStepCycle, instrument);
        return Concat(first, second);
    }

    private static List<Candle> Concat(List<Candle> first, List<Candle> second)
    {
        // Re-time the second segment to continue immediately after the first.
        var offset = first[^1].TimestampUtc.AddMinutes(15) - second[0].TimestampUtc;
        foreach (var candle in second) candle.TimestampUtc = candle.TimestampUtc.Add(offset);

        var combined = new List<Candle>(first.Count + second.Count);
        combined.AddRange(first);
        combined.AddRange(second);
        return combined;
    }
}
