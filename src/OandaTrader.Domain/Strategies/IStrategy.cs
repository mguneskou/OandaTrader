using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Strategies;

public interface IStrategy
{
    string Name { get; }

    /// <summary>Returns a trade decision for the just-closed candle, or null if there's no signal.</summary>
    TradeDecision? Evaluate(StrategyContext context);
}
