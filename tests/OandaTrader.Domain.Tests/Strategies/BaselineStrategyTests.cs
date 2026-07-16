using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Strategies;
using OandaTrader.Domain.Tests.TestSupport;
using Xunit;

namespace OandaTrader.Domain.Tests.Strategies;

public class BaselineStrategyTests
{
    [Fact]
    public void Evaluate_WithInsufficientHistory_ReturnsNull()
    {
        var candles = CandleFactory.GenerateTrend(FeatureBuilder.MinimumCandleCount - 1, 1.1000m, 0.0001m);
        var strategy = new BaselineStrategy();

        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = candles });

        Assert.Null(decision);
    }

    [Fact]
    public void Evaluate_AtBullishEmaCrossoverWithModerateRsi_ReturnsLongDecision()
    {
        // A mild downtrend (EMA20 < EMA50) followed by a moderate uptrend, truncated to the
        // exact bar where EMA20 crosses above EMA50 (located via the same Indicators engine
        // BaselineStrategy itself uses, so this test doesn't depend on hand-computed EMA values).
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: -0.00005m,
            secondCount: 80, secondStepCycle: [0.00025m, 0.00025m, -0.00015m, 0.00020m, -0.00015m]);

        int crossIndex = FindEmaCrossoverIndex(candles, bullish: true);
        var truncated = candles.Take(crossIndex + 1).ToList();

        double rsiAtCross = Indicators.Rsi(truncated.Select(c => (double)c.Close).ToList(), 14)[^1];
        Assert.True(rsiAtCross < 70, $"Test setup invariant: expected a moderate (non-overbought) RSI at the cross, got {rsiAtCross:F1}.");

        var strategy = new BaselineStrategy();
        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = truncated });

        Assert.NotNull(decision);
        Assert.Equal(TradeDirection.Long, decision!.Direction);
        Assert.True(decision.StopLoss < decision.EntryPrice, "Long stop-loss should sit below entry.");
        Assert.True(decision.TakeProfit > decision.EntryPrice, "Long take-profit should sit above entry.");
        Assert.Contains("crossed above", decision.ReasoningText);
    }

    [Fact]
    public void Evaluate_AtBearishEmaCrossoverWithModerateRsi_ReturnsShortDecision()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: 0.00005m,
            secondCount: 80, secondStepCycle: [-0.00025m, -0.00025m, 0.00015m, -0.00020m, 0.00015m]);

        int crossIndex = FindEmaCrossoverIndex(candles, bullish: false);
        var truncated = candles.Take(crossIndex + 1).ToList();

        double rsiAtCross = Indicators.Rsi(truncated.Select(c => (double)c.Close).ToList(), 14)[^1];
        Assert.True(rsiAtCross > 30, $"Test setup invariant: expected a moderate (non-oversold) RSI at the cross, got {rsiAtCross:F1}.");

        var strategy = new BaselineStrategy();
        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = truncated });

        Assert.NotNull(decision);
        Assert.Equal(TradeDirection.Short, decision!.Direction);
        Assert.True(decision.StopLoss > decision.EntryPrice, "Short stop-loss should sit above entry.");
        Assert.True(decision.TakeProfit < decision.EntryPrice, "Short take-profit should sit below entry.");
        Assert.Contains("crossed below", decision.ReasoningText);
    }

    [Fact]
    public void Evaluate_OneBarBeforeCrossover_ReturnsNull()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: -0.00005m,
            secondCount: 80, secondStepCycle: [0.00025m, 0.00025m, -0.00015m, 0.00020m, -0.00015m]);

        int crossIndex = FindEmaCrossoverIndex(candles, bullish: true);
        var truncated = candles.Take(crossIndex).ToList(); // one bar short of the cross

        var strategy = new BaselineStrategy();
        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = truncated });

        Assert.Null(decision);
    }

    /// <summary>Finds the first index at which EMA20 crosses EMA50 in the given direction,
    /// using the same Indicators.Ema implementation the strategy relies on.</summary>
    private static int FindEmaCrossoverIndex(IReadOnlyList<Candle> candles, bool bullish)
    {
        var closes = candles.Select(c => (double)c.Close).ToList();
        var emaFast = Indicators.Ema(closes, 20);
        var emaSlow = Indicators.Ema(closes, 50);

        for (int i = 1; i < closes.Count; i++)
        {
            if (double.IsNaN(emaFast[i - 1]) || double.IsNaN(emaSlow[i - 1])) continue;

            bool crossedUp = emaFast[i - 1] <= emaSlow[i - 1] && emaFast[i] > emaSlow[i];
            bool crossedDown = emaFast[i - 1] >= emaSlow[i - 1] && emaFast[i] < emaSlow[i];

            if (bullish && crossedUp) return i;
            if (!bullish && crossedDown) return i;
        }

        throw new InvalidOperationException("Test setup failure: no EMA crossover found in the generated series.");
    }
}
