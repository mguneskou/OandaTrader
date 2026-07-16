using OandaTrader.Domain.Features;
using OandaTrader.Domain.Tests.TestSupport;
using Xunit;

namespace OandaTrader.Domain.Tests.Features;

public class FeatureBuilderTests
{
    [Fact]
    public void Build_WithInsufficientHistory_Throws()
    {
        var candles = CandleFactory.GenerateTrend(FeatureBuilder.MinimumCandleCount - 1, 1.1000m, 0.0001m);
        Assert.Throws<ArgumentException>(() => FeatureBuilder.Build(candles));
    }

    [Fact]
    public void Build_WithSufficientHistory_ProducesNoNaNValues()
    {
        var candles = CandleFactory.GenerateTrend(FeatureBuilder.MinimumCandleCount, 1.1000m, 0.0001m);
        var features = FeatureBuilder.Build(candles);

        Assert.False(double.IsNaN(features.Ema20));
        Assert.False(double.IsNaN(features.Ema50));
        Assert.False(double.IsNaN(features.Rsi14));
        Assert.False(double.IsNaN(features.Atr14));
        Assert.False(double.IsNaN(features.MacdLine));
        Assert.False(double.IsNaN(features.MacdSignal));
        Assert.False(double.IsNaN(features.BollingerWidth20));
        Assert.False(double.IsNaN(features.RecentVolatility20));
        Assert.False(double.IsNaN(features.PriceDistanceFromEma20Pct));
    }

    [Fact]
    public void Build_TimeFeatures_MatchLastCandleTimestamp()
    {
        var candles = CandleFactory.GenerateTrend(FeatureBuilder.MinimumCandleCount, 1.1000m, 0.0001m);
        var features = FeatureBuilder.Build(candles);
        var lastTimestamp = candles[^1].TimestampUtc;

        Assert.Equal(lastTimestamp.Hour, features.HourOfDayUtc);
        Assert.Equal((double)lastTimestamp.DayOfWeek, features.DayOfWeek);
    }
}
