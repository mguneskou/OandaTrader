using OandaTrader.Domain.Backtesting;
using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Strategies;
using OandaTrader.Domain.Tests.TestSupport;
using Xunit;

namespace OandaTrader.Domain.Tests.Backtesting;

public class BacktestEngineTests
{
    [Fact]
    public void Run_WithInsufficientCandles_ProducesNoTrades()
    {
        var candles = CandleFactory.GenerateTrend(FeatureBuilder.MinimumCandleCount - 1, 1.1000m, 0.0001m);

        var (trades, summary) = BacktestEngine.Run("EUR_USD", candles, new BaselineStrategy());

        Assert.Empty(trades);
        Assert.Equal(0, summary.TradeCount);
    }

    [Fact]
    public void Run_OverBullishSeries_FindsOnlyLongTradesWithConsistentSummary()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: -0.00005m,
            secondCount: 160, secondStepCycle: [0.00025m, 0.00025m, -0.00015m, 0.00020m, -0.00015m]);

        var (trades, summary) = BacktestEngine.Run("EUR_USD", candles, new BaselineStrategy());

        Assert.NotEmpty(trades);
        Assert.All(trades, t => Assert.Equal(TradeDirection.Long, t.Direction));
        Assert.All(trades, t => Assert.True(t.Outcome is TradeOutcomeResult.Win or TradeOutcomeResult.Loss));

        Assert.Equal(trades.Count, summary.TradeCount);
        Assert.Equal(trades.Count(t => t.Outcome == TradeOutcomeResult.Win), summary.Wins);
        Assert.Equal(trades.Count(t => t.Outcome == TradeOutcomeResult.Loss), summary.Losses);
        Assert.Equal(trades.Sum(t => t.PnLInR), summary.TotalPnLInR);
    }

    [Fact]
    public void Run_OverBearishSeries_FindsOnlyShortTradesWithConsistentSummary()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: 0.00005m,
            secondCount: 160, secondStepCycle: [-0.00025m, -0.00025m, 0.00015m, -0.00020m, 0.00015m]);

        var (trades, summary) = BacktestEngine.Run("EUR_USD", candles, new BaselineStrategy());

        Assert.NotEmpty(trades);
        Assert.All(trades, t => Assert.Equal(TradeDirection.Short, t.Direction));
        Assert.Equal(trades.Count, summary.TradeCount);
    }

    [Fact]
    public void Run_EachTrade_HasNonEmptyFeaturesAndReasoning()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: -0.00005m,
            secondCount: 160, secondStepCycle: [0.00025m, 0.00025m, -0.00015m, 0.00020m, -0.00015m]);

        var (trades, _) = BacktestEngine.Run("EUR_USD", candles, new BaselineStrategy());

        Assert.NotEmpty(trades);
        Assert.All(trades, t =>
        {
            Assert.False(double.IsNaN(t.Features.Ema20));
            Assert.False(string.IsNullOrWhiteSpace(t.ReasoningText));
        });
    }
}
