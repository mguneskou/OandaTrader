using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Strategies;

/// <summary>
/// The learning strategy (see plan): BaselineStrategy generates candidate signals exactly as
/// it does in the backtest, and the trained model gates each candidate by its predicted win
/// probability. Candidates scoring below the confidence threshold are dropped. This keeps
/// training and serving symmetric — the model was trained on baseline-signal outcomes, so it
/// scores exactly the population of signals it will see live.
/// </summary>
public class MlStrategy(IWinProbabilityPredictor predictor, decimal confidenceThreshold) : IStrategy
{
    private readonly BaselineStrategy _candidateGenerator = new();

    public string Name => "ML";

    public TradeDecision? Evaluate(StrategyContext context)
    {
        var candidate = _candidateGenerator.Evaluate(context);
        if (candidate is null) return null;

        if (!predictor.IsReady)
        {
            // No trained model yet: refuse to trade rather than trading unscored.
            return null;
        }

        double winProbability = predictor.PredictWinProbability(candidate.Features);
        if (winProbability < (double)confidenceThreshold)
        {
            return null;
        }

        return new TradeDecision
        {
            Instrument = candidate.Instrument,
            Direction = candidate.Direction,
            EntryPrice = candidate.EntryPrice,
            StopLoss = candidate.StopLoss,
            TakeProfit = candidate.TakeProfit,
            Features = candidate.Features,
            MlConfidence = winProbability,
            ReasoningText = $"{candidate.ReasoningText} Model win probability {winProbability:P1} >= threshold {confidenceThreshold:P0}.",
        };
    }
}
