using OandaTrader.Domain.Features;

namespace OandaTrader.Domain.Strategies;

/// <summary>Abstraction over the trained ML model so Domain stays free of ML.NET.
/// Implemented in Infrastructure by the model prediction service.</summary>
public interface IWinProbabilityPredictor
{
    /// <summary>True when a trained model is loaded and able to score.</summary>
    bool IsReady { get; }

    /// <summary>Predicted probability (0..1) that a trade taken on these features wins.</summary>
    double PredictWinProbability(FeatureVector features);
}
