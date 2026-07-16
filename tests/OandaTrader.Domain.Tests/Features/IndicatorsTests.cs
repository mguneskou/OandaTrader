using OandaTrader.Domain.Features;
using OandaTrader.Domain.Tests.TestSupport;
using Xunit;

namespace OandaTrader.Domain.Tests.Features;

public class IndicatorsTests
{
    [Fact]
    public void Sma_OfConstantSeries_EqualsTheConstant()
    {
        var values = Enumerable.Repeat(1.5, 30).ToList();
        var sma = Indicators.Sma(values, 10);
        Assert.Equal(1.5, sma[^1], precision: 10);
    }

    [Fact]
    public void Sma_BeforePeriod_IsNaN()
    {
        var values = Enumerable.Repeat(1.0, 5).ToList();
        var sma = Indicators.Sma(values, 10);
        Assert.All(sma, v => Assert.True(double.IsNaN(v)));
    }

    [Fact]
    public void Ema_OfConstantSeries_ConvergesToTheConstant()
    {
        var values = Enumerable.Repeat(2.0, 60).ToList();
        var ema = Indicators.Ema(values, 20);
        Assert.Equal(2.0, ema[^1], precision: 10);
    }

    [Fact]
    public void Rsi_AllUpMoves_Is100()
    {
        var values = Enumerable.Range(0, 30).Select(i => (double)i).ToList(); // strictly increasing
        var rsi = Indicators.Rsi(values, 14);
        Assert.Equal(100, rsi[^1], precision: 6);
    }

    [Fact]
    public void Rsi_AllDownMoves_Is0()
    {
        var values = Enumerable.Range(0, 30).Select(i => (double)-i).ToList(); // strictly decreasing
        var rsi = Indicators.Rsi(values, 14);
        Assert.Equal(0, rsi[^1], precision: 6);
    }

    [Fact]
    public void Atr_OfVolatileSeries_IsPositive()
    {
        var candles = CandleFactory.GenerateTrend(30, 1.1000m, 0.0010m);
        var atr = Indicators.Atr(candles, 14);
        Assert.True(atr[^1] > 0);
    }

    [Fact]
    public void BollingerWidth_OfConstantSeries_IsZero()
    {
        var values = Enumerable.Repeat(1.0, 30).ToList();
        var width = Indicators.BollingerWidth(values, 20);
        Assert.Equal(0, width[^1], precision: 10);
    }

    [Fact]
    public void StdevOfReturns_OfConstantSeries_IsZero()
    {
        var values = Enumerable.Repeat(1.0, 30).ToList();
        var stdev = Indicators.StdevOfReturns(values, 20);
        Assert.Equal(0, stdev[^1], precision: 10);
    }

    [Fact]
    public void Macd_OfConstantSeries_IsZero()
    {
        var values = Enumerable.Repeat(1.0, 60).ToList();
        var (macdLine, signalLine, histogram) = Indicators.Macd(values);
        Assert.Equal(0, macdLine[^1], precision: 10);
        Assert.Equal(0, signalLine[^1], precision: 10);
        Assert.Equal(0, histogram[^1], precision: 10);
    }
}
