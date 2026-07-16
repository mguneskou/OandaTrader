using OandaTrader.Domain.Features;

namespace OandaTrader.Domain.Models;

/// <summary>A strategy's output: a trade to take, with the reasoning and feature snapshot
/// that justified it. Null from IStrategy.Evaluate means "no signal this bar".</summary>
public class TradeDecision
{
    public required string Instrument { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal StopLoss { get; init; }
    public required decimal TakeProfit { get; init; }
    public required string ReasoningText { get; init; }
    public required FeatureVector Features { get; init; }

    /// <summary>Predicted win-probability from MlStrategy; null for BaselineStrategy decisions.</summary>
    public double? MlConfidence { get; init; }
}
