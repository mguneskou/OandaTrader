using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Strategies;

/// <summary>Everything a strategy needs to evaluate one instrument at one candle close.</summary>
public class StrategyContext
{
    public required string Instrument { get; init; }

    /// <summary>Candle history, oldest first, newest (just-closed) last.</summary>
    public required IReadOnlyList<Candle> History { get; init; }
}
