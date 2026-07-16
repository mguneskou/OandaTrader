using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Strategies;
using OandaTrader.Domain.Tests.TestSupport;
using Xunit;

namespace OandaTrader.Domain.Tests.Strategies;

public class MlStrategyTests
{
    private sealed class FakePredictor(bool ready, double probability) : IWinProbabilityPredictor
    {
        public bool IsReady => ready;
        public double PredictWinProbability(FeatureVector features) => probability;
    }

    /// <summary>A candle series where BaselineStrategy fires a long signal on the last bar
    /// (same fixture construction as BaselineStrategyTests).</summary>
    private static List<Candle> BullishCrossoverSeries()
    {
        var candles = CandleFactory.GenerateTrendThenChoppy(
            firstCount: 80, firstStep: -0.00005m,
            secondCount: 80, secondStepCycle: [0.00025m, 0.00025m, -0.00015m, 0.00020m, -0.00015m]);

        var closes = candles.Select(c => (double)c.Close).ToList();
        var emaFast = Indicators.Ema(closes, 20);
        var emaSlow = Indicators.Ema(closes, 50);
        for (int i = 1; i < closes.Count; i++)
        {
            if (double.IsNaN(emaFast[i - 1]) || double.IsNaN(emaSlow[i - 1])) continue;
            if (emaFast[i - 1] <= emaSlow[i - 1] && emaFast[i] > emaSlow[i])
            {
                return candles.Take(i + 1).ToList();
            }
        }
        throw new InvalidOperationException("Test setup failure: no crossover found.");
    }

    [Fact]
    public void Evaluate_HighConfidencePrediction_PassesGateWithConfidenceAttached()
    {
        var strategy = new MlStrategy(new FakePredictor(ready: true, probability: 0.80), confidenceThreshold: 0.55m);

        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = BullishCrossoverSeries() });

        Assert.NotNull(decision);
        Assert.Equal(0.80, decision!.MlConfidence);
        Assert.Contains("win probability", decision.ReasoningText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_LowConfidencePrediction_IsGatedOut()
    {
        var strategy = new MlStrategy(new FakePredictor(ready: true, probability: 0.40), confidenceThreshold: 0.55m);

        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = BullishCrossoverSeries() });

        Assert.Null(decision);
    }

    [Fact]
    public void Evaluate_PredictorNotReady_RefusesToTrade()
    {
        var strategy = new MlStrategy(new FakePredictor(ready: false, probability: 0.99), confidenceThreshold: 0.55m);

        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = BullishCrossoverSeries() });

        Assert.Null(decision);
    }

    [Fact]
    public void Evaluate_NoBaselineSignal_ReturnsNullWithoutScoring()
    {
        // Flat series produces no crossover, so the candidate generator yields nothing.
        var candles = CandleFactory.GenerateTrend(100, 1.1000m, 0m);
        var strategy = new MlStrategy(new FakePredictor(ready: true, probability: 0.99), confidenceThreshold: 0.55m);

        var decision = strategy.Evaluate(new StrategyContext { Instrument = "EUR_USD", History = candles });

        Assert.Null(decision);
    }
}
